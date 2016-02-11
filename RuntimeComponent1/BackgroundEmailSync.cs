using Windows.ApplicationModel.Background;

namespace BackgroundEmailSync
{
    /// <summary>
    /// A background task which wathces for email triage events, 
    /// and force syncs them.
    /// </summary>
    public class BackgroundEmailSync : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            EmailManager.Instance.initState();
        }
    }
}
