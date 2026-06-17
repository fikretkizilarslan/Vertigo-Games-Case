using System;
using UnityEngine;
using TMPro;

namespace BattlePass.UI
{
    /// <summary>
    /// Partial class — Wallet management: currency wallet resolution, balance crediting
    /// and the DOTween fly-animation bridge.
    ///
    /// Planned extraction from BattlePassManager.cs. The wallet methods (CreditClaimedReward,
    /// AnimateWalletChange, IsDiamondReward/IsGoldReward/IsLuckyGemReward, the three Resolve*
    /// helpers and their Refresh* counterparts) currently live in the main BattlePassManager.cs
    /// file and will be moved here in a follow-up pass once the extraction has been validated
    /// against a device build.
    ///
    /// This file already holds the partial class declaration so new wallet utilities can be
    /// added here immediately.
    /// </summary>
    public partial class BattlePassManager
    {
        // Wallet management methods live in BattlePassManager.cs pending extraction.
        // See: CreditClaimedReward, AnimateWalletChange, IsDiamondReward, IsGoldReward,
        //      IsLuckyGemReward, RewardNameContains, ResolveDiamondWallet, ResolveGoldWallet,
        //      ResolveGemWallet, RefreshDiamondCounter, RefreshGoldCounter, RefreshGemCounter,
        //      CreditLuckyGemReward, EnsureGemWalletController, EnsureWalletFlyAnimator.
    }
}
