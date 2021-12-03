using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using PropertyChanged;

namespace PartNumberChangeDetector
{
    [DoNotNotify]
    public class RangeObservableCollection<T> : ObservableCollection<T>
    {
        private bool suppressNotification = false;

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!suppressNotification) base.OnCollectionChanged(e);
        }

        public void AddRange(IEnumerable<T> items)
        {
            if (items == null) return;

            suppressNotification = true;

            foreach (var item in items)
            {
                Add(item);
            }

            suppressNotification = false;

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void AddUnique(IEnumerable<T> items)
        {
            if (items == null) return;

            var hashSet = this.ToHashSet();

            AddRange(items.Distinct().Where(i => !hashSet.Contains(i)));
        }

        public void RemoveRange(IEnumerable<T> items)
        {
            if (items == null) return;

            suppressNotification = true;

            //var removedItems = new List<T>();

            foreach (var item in items)
            {
                //if (Remove(item)) removedItems.Add(item);
                Remove(item);
            }

            suppressNotification = false;

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
