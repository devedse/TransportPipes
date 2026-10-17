using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;

namespace TransportPipes.Backend
{
    public class SuperLocker
    {
        private static ReaderWriterLockSlim _staticLocker;
        private static ReaderWriterLockSlim StaticLocker
        {
            get
            {
                if (_staticLocker == null)
                {
                    _staticLocker = new ReaderWriterLockSlim();
                }
                return _staticLocker;
            }
        }

        public Boolean HasReadLock { get; private set; }
        public Boolean HasWriteLock { get; private set; }

        private List<String> _logging = new List<String>();
        private List<String> _loggingVerbose = new List<String>();

        public SuperLocker()
        {

        }

        private void AddLogLine(String line, Boolean verbose)
        {
            if (verbose)
            {
                _loggingVerbose.Add(DateTime.Now + ": " + line);
            }
            else
            {
                _logging.Add(DateTime.Now + ": " + line);
            }
        }

        public void RequestReadLock()
        {
            AddLogLine("RequestReadLock", true);

            if (HasReadLock)
            {
                //We already have a read lock
            }
            else if (HasWriteLock)
            {
                //We already have a write lock so we can read as well
            }
            else
            {
                StaticLocker.EnterReadLock();
                HasReadLock = true;

                AddLogLine("Entered Read Lock", false);
            }
        }

        public void RequestWriteLock()
        {
            AddLogLine("RequestWriteLock", true);

            if (HasReadLock)
            {
                StaticLocker.ExitReadLock();
                HasReadLock = false;
                StaticLocker.EnterWriteLock();
                HasWriteLock = true;

                AddLogLine("Entered Write Lock", false);
            }
            else if (HasWriteLock)
            {
                //We already have a write lock so we can read as well
            }
            else
            {
                StaticLocker.EnterWriteLock();
                HasWriteLock = true;

                AddLogLine("Entered Write Lock", false);
            }
        }

        public void DowngradeToReadLock()
        {
            AddLogLine("DowngradeToReadLock", true);

            if (HasWriteLock)
            {
                StaticLocker.ExitWriteLock();
                HasWriteLock = false;
            }
            if (!HasReadLock)
            {
                StaticLocker.EnterReadLock();
                HasReadLock = true;

                AddLogLine("Downgraded to Read Lock", false);
            }
        }

        public String ReleaseLocks()
        {
            AddLogLine("ReleaseLocks", true);

            if (HasReadLock)
            {
                StaticLocker.ExitReadLock();
                HasReadLock = false;

                AddLogLine("Left Read Lock", false);
            }
            if (HasWriteLock)
            {
                StaticLocker.ExitWriteLock();
                HasWriteLock = false;

                AddLogLine("Left Write Lock", false);
            }

            var sb = new StringBuilder();
            sb.AppendLine("Lock Logging:");
            sb.AppendLine(String.Join(Environment.NewLine, _logging));
            //sb.AppendLine();

            //sb.AppendLine("Verbose Lock Logging:");
            //sb.AppendLine(String.Join(Environment.NewLine, _loggingVerbose));

            return sb.ToString();
        }
    }
}