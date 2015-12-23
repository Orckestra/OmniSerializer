using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Orckestra.Serialization
{
    internal class ReferenceWatcher : IDisposable
    {
        [ThreadStatic]
        private static ReferenceWatcher watcher;
        [ThreadStatic]
        private static HashSet<object> trackedObjects;

        internal static ReferenceWatcher Current
        {
            get
            {
                return ReferenceWatcher.watcher;
            }
        }

        internal ReferenceWatcher()
        {
            if (Current != null)
            {
                throw new InvalidOperationException("Cannot instantiate a new ReferenceWatcher while there is an instance of ReferenceWatcher on the current thread.");
            }

            Thread.BeginThreadAffinity();
            ReferenceWatcher.watcher = this;
            ReferenceWatcher.trackedObjects = new HashSet<object>();
        }

        private bool disposed = false;
        public void Dispose()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(typeof(ReferenceWatcher).FullName);
            }
            disposed = true;
            ReferenceWatcher.watcher = null;
            ReferenceWatcher.trackedObjects = null;
            Thread.EndThreadAffinity();
        }

        internal void TrackObject(object obj)
        {
            if (obj == null)
            {
                return;
            }

            Debug.Assert(obj.GetType()
                            .IsClass);

            if (trackedObjects.Contains(obj))
            {
                throw new CircularReferenceException(string.Format("Object {0} has already been serialized possibly due to a circular reference.",
                                                                   obj.GetType()));
            }
            trackedObjects.Add(obj);
        }

        internal void RemoveObject(object obj)
        {
            if (obj == null)
            {
                return;
            }

            Debug.Assert(obj.GetType()
                            .IsClass);

            if (!trackedObjects.Contains(obj))
            {
                throw new InvalidOperationException(string.Format("Object {0} is not tracked by the ReferenceWatcher.",
                                                                   obj.GetType()));
            }
            trackedObjects.Remove(obj);
        }
    }
}
