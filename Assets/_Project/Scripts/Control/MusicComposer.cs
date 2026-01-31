using UnityEngine;

namespace TouchIT.Control
{
    public class MusicComposer
    {
        // 펜타토닉 스케일 (도, 레, 미, 솔, 라) -> 인덱스 오프셋
        // 0:C, 1:D, 2:E, 3:G, 4:A
        private readonly int[] _pentatonicOffsets = { 0, 1, 2, 4, 5 };

        // 이전 노트의 인덱스 (연결성을 위해)
        private int _lastNoteIndex = 0;

        public int GetNextNoteIndex()
        {
            // [알고리즘]
            // 이전 음에서 너무 멀리 튀지 않게 (-2 ~ +2 범위 내에서 이동)
            // 랜덤성을 주되 듣기 싫지 않게 만듦.

            int step = Random.Range(-2, 3); // -2, -1, 0, 1, 2
            int nextIndex = _lastNoteIndex + step;

            // 범위 제한 (0 ~ 4 사이 반복)
            if (nextIndex < 0) nextIndex = 4;
            if (nextIndex > 4) nextIndex = 0;

            _lastNoteIndex = nextIndex;
            return _pentatonicOffsets[nextIndex]; // 실제 스케일 인덱스 반환
        }

        // 콤보에 따른 화음 레벨 결정 (0:단음, 1:화음, 2:풀코드)
        public int GetIntensityLevel(int currentCombo)
        {
            if (currentCombo < 10) return 0;
            if (currentCombo < 30) return 1;
            return 2;
        }
    }
}