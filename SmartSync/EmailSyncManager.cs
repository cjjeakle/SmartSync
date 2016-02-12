using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Email;
using Windows.ApplicationModel.UserDataAccounts;

namespace SmartSync
{
    /// <summary>
    /// A singleton class which keeps track of any available email accounts,
    /// and provides an interface to keep them synced with the server.
    /// </summary>
    class EmailSyncManager
    {
        /*
        * Public Interface
        */

        public static EmailSyncManager Instance { get { return instance.Value; } }

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

        /*
        * Private Types
        */

        /// <summary>
        /// Maintains a guard boolean value and 
        /// provides an interface to atomically read and flip thay value.
        /// </summary>
        private class AtomicGuard
        {
            /// <summary>
            /// Creates a new atomic guard with a starting value of false.
            /// </summary>
            public AtomicGuard()
                : this(false)
            { }

            /// <summary>
            /// Creates a new atomic guard with a starting value set via 'initialValue'.
            /// </summary>
            /// <param name="initialValue">The value the guard will start with.</param>
            public AtomicGuard(bool initialValue)
            {
                mutex = new Mutex();
                guard = initialValue;
            }

            /// <summary>
            /// Atomically reads the current guard boolean value, flips that value, and returns it.
            /// </summary>
            /// <returns>The guard bool's value prior to being flipped.</returns>
            public bool atomicTestAndFlip()
            {
                mutex.WaitOne();
                bool previousGuardValue = guard;
                guard = !guard;
                mutex.ReleaseMutex();

                return previousGuardValue;
            }

            private Mutex mutex;
            private bool guard;
        }

        /*
        * Private Member Variables
        */

        private static readonly Lazy<EmailSyncManager> instance = new Lazy<EmailSyncManager>(() => new EmailSyncManager());
        private IReadOnlyList<EmailMailbox> mailboxes;
        private Dictionary<string, AtomicGuard> syncEnabled; // A table of atomically accessed guards used to prevent OnMailboxChanged's sync from invoking itself

        /*
        * Private Implementation
        */

        /// <summary>
        /// Constructs a new instance of the EmailManager class.
        /// Automatically assembles a list of email accounts on this machine,
        /// and attaches event listeners to each of them to detect and sync changes.
        /// </summary>
        private EmailSyncManager()
        {
            syncEnabled = new Dictionary<string, AtomicGuard>();
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
                syncEnabled.Add(mailbox.Id, new AtomicGuard(true));
                mailbox.MailboxChanged += OnMailboxChanged;
            }
        }

        /// <summary>
        /// An event handler to invoke a sync when a mailbox changes.
        /// </summary>
        /// <param name="sender">The email mailbox which changed.</param>
        /// <param name="args">Any arguments sent with the event.</param>
        private static void OnMailboxChanged(EmailMailbox sender, EmailMailboxChangedEventArgs args)
        {
            // Flip a guard used to keep the following sync from triggering this event handler infinitely
            if (Instance.syncEnabled[sender.Id].atomicTestAndFlip())
            {
                sender.SyncManager.SyncAsync();
            }
        }
    }
}
