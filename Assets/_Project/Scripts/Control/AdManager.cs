using UnityEngine;
using System;

namespace TouchIT.Control
{
    // 실제 AdMob 연동 전에는 더미(Dummy) 로직으로 테스트
    public class AdManager : MonoBehaviour
    {
        // 3판에 1번 전면광고 카운트
        private int _playCount = 0;
        private const int AD_FREQUENCY = 3;

        public void Initialize()
        {
            // TODO: GoogleMobileAds.Api.MobileAds.Initialize(...)
            Debug.Log("📺 AdManager Initialized");
        }

        // 보상형 광고 (부활용)
        public void ShowRewardAd(Action onRewarded, Action onFailed)
        {
            Debug.Log("📺 Show Reward Ad...");

            // [실제 연동 시]
            // if (rewardAd.IsLoaded()) rewardAd.Show();

            // [테스트용] 무조건 성공 처리
            bool isSuccess = true;
            if (isSuccess)
            {
                Debug.Log("📺 Ad Watched! Reward Given.");
                onRewarded?.Invoke();
            }
            else
            {
                onFailed?.Invoke();
            }
        }

        // 전면 광고 (게임 종료/포기 시 가끔)
        public void CheckAndShowInterstitial()
        {
            _playCount++;
            if (_playCount >= AD_FREQUENCY)
            {
                Debug.Log("📺 Show Interstitial Ad (Forced)");
                // ShowAd();
                _playCount = 0;
            }
        }
    }
}