using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Email;
using Windows.ApplicationModel.UserDataAccounts;
using Windows.Foundation;

namespace SmartSync
{
    class EmailManager
    {
        public static EmailManager Instance { get { return instance.Value; } }

        /// <summary>
        /// An empty method which is called to initialize the email manager
        /// by accessing the singleton instance.
        /// </summary>
        public void initState() { }

        /// <summary>
        /// Force-syncs every email account on the machine.
        /// </summary>
        public async void ForceSync()
        {
            foreach (var mailbox in mailboxes)
            {
                await mailbox.SyncManager.SyncAsync();
            }
        }

        public static void OnMailboxChanged(EmailMailbox sender, EmailMailboxChangedEventArgs args)
        {
            var task = Task.Run(async () => { await sender.SyncManager.SyncAsync(); });
            task.Wait();
        }

        private static readonly Lazy<EmailManager> instance = new Lazy<EmailManager>(() => new EmailManager());
        private IReadOnlyList<EmailMailbox> mailboxes;

        private EmailManager()
        {
            var task = Task.Run(async () => { mailboxes = await getAllMailboxes(); });
            task.Wait();
            AttachChangeListeners();
        }

        private static async Task<IReadOnlyList<EmailMailbox>> getAllMailboxes()
        {
            UserDataAccountStore store = await UserDataAccountManager.RequestStoreAsync(UserDataAccountStoreAccessType.AllAccountsReadOnly);

            IReadOnlyList<UserDataAccount> userAccounts = new List<UserDataAccount>().AsReadOnly();
            if (store != null)
            {
                userAccounts = await store.FindAccountsAsync();
            }

            List<EmailMailbox> localMailboxes = new List<EmailMailbox>();
            foreach (var account in userAccounts)
            {
                localMailboxes.AddRange(await account.FindEmailMailboxesAsync());
            }

            return localMailboxes.AsReadOnly();
        }

        /// <summary>
        /// Attaches an event listener to every email account on the machine.
        /// This listener forces a sync after any change to an email account.
        /// </summary>
        private void AttachChangeListeners()
        {
            foreach (var mailbox in mailboxes)
            {
                mailbox.MailboxChanged += OnMailboxChanged;
            }
        }
    }
}
