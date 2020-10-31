using LiveSplit.Model;
using System;

namespace LiveSplit.UI.Components
{
    public class MemeComponentFactory : IComponentFactory
    {
        public string ComponentName => "HP2 Nixxo Memerun";

        public string Description => "everytime you get a green split the game gets 10% slower and everytime you get a red split the game gets 10% faster.";

        public ComponentCategory Category => ComponentCategory.Other;

        public IComponent Create(LiveSplitState state) => new MemeComponent(state);

        public string UpdateName => ComponentName;

        public string XMLURL => null;

        public string UpdateURL => null;

        public Version Version => Version.Parse("1.0.0");
    }
}
