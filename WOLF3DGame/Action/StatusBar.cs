using System.Collections.Generic;
using Godot;
using System.Linq;
using System.Xml.Linq;
using System.Collections;
using System;

namespace WOLF3D.WOLF3DGame.Action
{
    public class StatusBar : Viewport, IDictionary<string, StatusNumber>, ICollection<KeyValuePair<string, StatusNumber>>, IEnumerable<KeyValuePair<string, StatusNumber>>, IEnumerable, IDictionary, ICollection, IReadOnlyDictionary<string, StatusNumber>, IReadOnlyCollection<KeyValuePair<string, StatusNumber>>
    {
        public StatusBar() : this(Assets.XML.Element("VgaGraph").Element("StatusBar")) { }
        public StatusBar(XElement xml)
        {
            Name = "StatusBar";
            Disable3d = true;
            RenderTargetClearMode = ClearMode.OnlyNextFrame;
            RenderTargetVFlip = true;
            XML = xml;
            ImageTexture pic = Assets.PicTextureSafe(XML.Attribute("Pic")?.Value);
            Size = pic.GetSize();
            AddChild(new Sprite()
            {
                Name = "StatusBarPic",
                Texture = pic,
                Position = Size / 2,
            });
            foreach (XElement number in XML.Elements("Number") ?? Enumerable.Empty<XElement>())
                Add(new StatusNumber(number));
        }

        private Dictionary<string, StatusNumber> StatusNumbers = new Dictionary<string, StatusNumber>();
        public void Add(StatusNumber statusNumber) => Add(statusNumber.Name, statusNumber);

        public bool Contains(string key) => StatusNumbers.ContainsKey(key);

        public void Add(string key, StatusNumber value)
        {
            AddChild(value);
            StatusNumbers.Add(key, value);
        }

        public void Clear()
        {
            foreach (StatusNumber statusNumber in StatusNumbers.Values)
                RemoveChild(statusNumber);
            StatusNumbers.Clear();
        }

        public IDictionaryEnumerator GetEnumerator() => StatusNumbers.GetEnumerator();

        public void Remove(string key) => RemoveBool(key);

        public bool RemoveBool(string key)
        {
            if (StatusNumbers.TryGetValue(key, out StatusNumber statusNumber))
            {
                RemoveChild(statusNumber);
                return StatusNumbers.Remove(key);
            }
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator() => StatusNumbers.GetEnumerator();

        public bool ContainsKey(string key) => StatusNumbers.ContainsKey(key);

        public bool TryGetValue(string key, out StatusNumber value) => StatusNumbers.TryGetValue(key, out value);

        bool IDictionary<string, StatusNumber>.Remove(string key) => RemoveBool(key);

        public void Add(KeyValuePair<string, StatusNumber> item) => Add(item.Key, item.Value);

        public bool Contains(KeyValuePair<string, StatusNumber> item) => StatusNumbers.Contains(item);

        public bool Remove(KeyValuePair<string, StatusNumber> item) => RemoveBool(item.Key);

        IEnumerator<KeyValuePair<string, StatusNumber>> IEnumerable<KeyValuePair<string, StatusNumber>>.GetEnumerator() => (IEnumerator<KeyValuePair<string, StatusNumber>>)StatusNumbers;

        public void CopyTo(KeyValuePair<string, StatusNumber>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, StatusNumber>>)StatusNumbers).CopyTo(array, arrayIndex);
        }

        public bool Contains(object key) => key is string @string && StatusNumbers.ContainsKey(@string);

        public void Add(object key, object value)
        {
            if (key is string @string && value is StatusNumber statusNumber)
                Add(@string, statusNumber);
        }

        public void Remove(object key)
        {
            if (key is string @string)
                RemoveBool(@string);
        }

        public void CopyTo(Array array, int index) => ((ICollection)StatusNumbers).CopyTo(array, index);

        public XElement XML { get; set; }

        public ICollection Keys => StatusNumbers.Keys;

        public ICollection Values => StatusNumbers.Values;

        public int Count => StatusNumbers.Count;

        ICollection<StatusNumber> IDictionary<string, StatusNumber>.Values => StatusNumbers.Values;

        IEnumerable<string> IReadOnlyDictionary<string, StatusNumber>.Keys => StatusNumbers.Keys;

        IEnumerable<StatusNumber> IReadOnlyDictionary<string, StatusNumber>.Values => StatusNumbers.Values;

        ICollection<string> IDictionary<string, StatusNumber>.Keys => StatusNumbers.Keys;

        bool ICollection<KeyValuePair<string, StatusNumber>>.IsReadOnly => ((ICollection<KeyValuePair<string, StatusNumber>>)StatusNumbers).IsReadOnly;

        public bool IsReadOnly => ((IDictionary)StatusNumbers).IsReadOnly;

        public bool IsFixedSize => ((IDictionary)StatusNumbers).IsFixedSize;

        public object SyncRoot => ((ICollection)StatusNumbers).SyncRoot;

        public bool IsSynchronized => ((ICollection)StatusNumbers).IsSynchronized;

        public object this[object key] { get => ((IDictionary)StatusNumbers)[key]; set => ((IDictionary)StatusNumbers)[key] = value; }
        public StatusNumber this[string key]
        {
            get => StatusNumbers[key];
            set => Add(key, value);
        }
    }
}
