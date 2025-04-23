using UnityEngine;

namespace LazyRedpaw.StaticHashes
{
    public struct HashLabelPair
    {
        private readonly int _hash;
        private readonly GUIContent _label;

        public int Hash => _hash;
        public GUIContent Label => _label;

        public HashLabelPair(int hash, string name)
        {
            _hash = hash;
            _label = new GUIContent(name);
        }
    }
}