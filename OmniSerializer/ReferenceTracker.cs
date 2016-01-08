using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Orckestra.OmniSerializer
{
    /// <summary>
    /// Allows an object to be tracked in a graph to detect circular references
    /// </summary>
    public class ReferenceTracker : IDisposable
    {
        [ThreadStatic]
        private static ReferenceTracker _tracker;
        [ThreadStatic]
        private static HashSet<object> _trackedObjects;

        public static ReferenceTracker Current
        {
            get
            {
                return ReferenceTracker._tracker;
            }
        }

        public ReferenceTracker()
        {
            if (Current != null)
            {
                throw new InvalidOperationException("Cannot instantiate a new ReferenceTracker while there is an instance of ReferenceTracker on the current thread.");
            }

            Thread.BeginThreadAffinity();
            ReferenceTracker._tracker = this;
            ReferenceTracker._trackedObjects = new HashSet<object>();
        }

        private bool _disposed = false;
        public void Dispose()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(typeof(ReferenceTracker).FullName);
            }
            _disposed = true;
            ReferenceTracker._tracker = null;
            ReferenceTracker._trackedObjects = null;
            Thread.EndThreadAffinity();
        }

        public void TrackObject(object obj)
        {
            if (obj == null)
            {
                return;
            }

            Debug.Assert(obj.GetType().IsClass || obj.GetType().IsInterface);

            if (_trackedObjects.Contains(obj))
            {
                throw new ObjectExistsInCurrentSerializationGraphException(string.Format("Object {0} has already been serialized.  It is usually caused by a circular reference.  Consider changing the serialization engine to use BinaryFormatter for this type or its root type.",
                                                                                         obj.GetType()));
            }
            _trackedObjects.Add(obj);
        }

        public void RemoveObject(object obj)
        {
            if (obj == null)
            {
                return;
            }

            Debug.Assert(obj.GetType().IsClass || obj.GetType().IsInterface);

            _trackedObjects.Remove(obj);
        }
        public bool IsTracked(object obj)
        {
            return _trackedObjects.Contains(obj);
        }
    }
}
