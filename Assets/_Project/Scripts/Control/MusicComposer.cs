using UnityEngine;

namespace TouchIT.Control
{
    public class MusicComposer
    {
        private readonly int[] _pentatonic = { 0, 1, 2, 4, 5 };
        private int _lastIdx = 0;

        public int GetNextNoteIndex()
        {
            int step = Random.Range(-2, 3);
            int next = _lastIdx + step;
            if (next < 0) next = 4;
            if (next > 4) next = 0;
            _lastIdx = next;
            return _pentatonic[next];
        }
    }
}