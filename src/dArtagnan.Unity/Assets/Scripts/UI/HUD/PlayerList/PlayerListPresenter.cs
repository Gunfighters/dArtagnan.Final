using System.Linq;
using R3;

namespace UI.HUD.PlayerList
{
    public static class PlayerListPresenter
    {
        public static void Initialize(PlayerListModel model, PlayerListView view)
        {
            model.playerList.Subscribe(arr =>
            {
                while (view.ShownItems.Any()) view.ReturnToPool(view.ShownItems.Pop());
                foreach (var m in arr)
                {
                    var item = view.GetNewItem();
                    item.Initialize(m);
                }
            });
        }
    }
}