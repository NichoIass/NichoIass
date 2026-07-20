namespace ProvisionTool.Utils
{
    /// <summary>
    /// Расширения для работы с коллекциями
    /// </summary>
    public static class CollectionExtensions
    {
        public static void AddRange<T>(this System.Collections.ObjectModel.ObservableCollection<T> collection, IEnumerable<T> items)
        {
            foreach (var item in items)
                collection.Add(item);
        }

        public static void ClearAndAddRange<T>(this System.Collections.ObjectModel.ObservableCollection<T> collection, IEnumerable<T> items)
        {
            collection.Clear();
            collection.AddRange(items);
        }
    }
}
