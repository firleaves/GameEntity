using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public interface IUISystem
    {
        UniTask<TUI> OpenAsync<TUI>(UIOpenParams options = null, CancellationToken ct = default)
            where TUI : UIEntity, new();

        UniTask CloseAsync(UIEntity ui, UICloseReason reason = UICloseReason.User);
        UniTask CloseGroupAsync(string group, UICloseReason reason = UICloseReason.User);
        TUI Get<TUI>() where TUI : UIEntity;
        bool TryGet<TUI>(out TUI ui) where TUI : UIEntity;
        UISystemSnapshot GetSnapshot();
    }

}
