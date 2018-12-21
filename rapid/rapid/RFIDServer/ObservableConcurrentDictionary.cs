using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.Distributions;

namespace RFID
{
    class ObservableConcurrentDictionary<T> where T : IDistribution
    {
        // The collection that this class wraps to be observable.
        ConcurrentDictionary<string, T> collection;
        // Wrapper for the function that acts on a copy of the collection when invoked.
        Action<ConcurrentDictionary<string, T>> handler;

        public void replaceCollectionWith(ConcurrentDictionary<string, T> newCollection, bool shouldInvokeHandler)
        {
            // Expect that the caller cloned newCollection before passing it in.
            this.collection = newCollection;
            if (shouldInvokeHandler)
            {
                this.invokeHandler();
            }
        }

        // Invokes the handler function.
        public void invokeHandler()
        {
            this.handler(this.collection);
        }

        // Sets the handler function for handling replacing the collection.
        public void setHandler(Action<ConcurrentDictionary<string, T>> handler)
        {
            this.handler = handler;
        }
    }
}

