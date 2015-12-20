using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace SmartSync
{
    /// <summary>
    /// A background tasks which wathces for email triage events, 
    /// and force syncs them.
    /// </summary>
    public sealed class SmartSync : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            EmailManager.Instance.ForceSync();
        }
    }
}
