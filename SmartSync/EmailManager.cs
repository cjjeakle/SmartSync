using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Email;
using Windows.ApplicationModel.UserDataAccounts;

namespace SmartSync
{
    /// <summary>
    /// A singleton class which keeps track of any available email accounts,
    /// and provides an interface to keep them synced with the server.
    /// </summary>
    class EmailManager
    {
        public static EmailManager Instance { get { return instance.Value; } }

        /// <summary>
        /// An empty method which is called to initialize the email manager
        /// by accessing the singleton instance.
        /// </summary>
        public void initState() { }

        /// <summary>
        /// Invokes a sync for every email account on the machine.
        /// </summary>
        public async void SyncAllAccountsAsync()
        {
            foreach (var mailbox in mailboxes)
            {
                await mailbox.SyncManager.SyncAsync();
            }
        }

        /// <summary>
        /// An event handler to invoke a sync when a mailbox changes.
        /// </summary>
        /// <param name="sender">The email mailbox which changed.</param>
        /// <param name="args">Any arguments sent with the event.</param>
        public static void OnMailboxChanged(EmailMailbox sender, EmailMailboxChangedEventArgs args)
        {
            bool syncEnabled = Instance.syncEnabled[sender.Id];

            // Flip the guard used to keep the following sync from triggering this event handler infinitely
            Instance.syncEnabled[sender.Id] = !syncEnabled;

            if (syncEnabled)
            {
                sender.SyncManager.SyncAsync();
            }

            System.Diagnostics.Debug.WriteLine("Test");
        }

        private static readonly Lazy<EmailManager> instance = new Lazy<EmailManager>(() => new EmailManager());
        private IReadOnlyList<EmailMailbox> mailboxes;
        private Dictionary<string, bool> syncEnabled; // A table of gurads used to prevent OnMailboxChanged's sync from invoking itself

        /// <summary>
        /// Constructs a new instance of the EmailManager class.
        /// Automatically assembles a list of email accounts on this machine,
        /// and attaches event listeners to each of them to detect and sync changes.
        /// </summary>
        private EmailManager()
        {
            syncEnabled = new Dictionary<string, bool>();
            var task = Task.Run(async () => { mailboxes = await GetAllMailboxesAsync(); });
            task.Wait();
            AttachChangeListeners();
        }

        /// <summary>
        /// Asynchronously gets every mailbox available on the machine.
        /// </summary>
        /// <returns>A list of email accounts on this machine.</returns>
        private static async Task<IReadOnlyList<EmailMailbox>> GetAllMailboxesAsync()
        {
            var accountManager = await UserDataAccountManager.RequestStoreAsync(UserDataAccountStoreAccessType.AllAccountsReadOnly);
            var userAccounts = await accountManager.FindAccountsAsync();

            List<EmailMailbox> availableMailboxes = new List<EmailMailbox>();
            foreach (var account in userAccounts)
            {
                
                availableMailboxes.AddRange(await account.FindEmailMailboxesAsync());
            }

            return availableMailboxes.AsReadOnly();
        }

        /// <summary>
        /// Attaches an event listener to every email account on the machine.
        /// This listener invokes a sync after any change to an email account.
        /// </summary>
        private void AttachChangeListeners()
        {
            foreach (var mailbox in mailboxes)
            {
                syncEnabled.Add(mailbox.Id, true);
                mailbox.MailboxChanged += OnMailboxChanged;
            }
        }
    }
}
