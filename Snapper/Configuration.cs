using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace Snapper
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;

        public string WorkingDirectory { get; set; } = string.Empty;

        public string FallBackGlamourerString { get; set; } = "Bh+LCAAAAAAAAArEV9ly2jAU/Rc/Mxkv8pa3hoRAJ6QZoMmzAhfQRNiuJSdDM/n3XsmYeMNl2nr6Joujc+7mY/FujBiHR0gFiyPj0hoYNz8yluwgksbluzGlLBrTaKXWEwm7Ca4cYrr2wBimIBCzplzAwPiSJHxvXMo0Kx7mEs9WdqonDr+bh5WNy4+B8W29rusROySh54WW9Rei+U675hhoi6BveaQnwat4tS8L2mHg95UbFlNUuufapK/m3cGmquWYZl9aIwBZ0fLd3vK6oaloGxA36EvxHpYvrYp+X4pPKROyPUuvL83ZiEUbSFtF3d6G9H+Ijqke1vk2fquczR8Q8MhEnMckFvFmw2HVjnsCmiijLriacX5og4ky3ctbToUAvdR7KluzAR9mQsY79hO05ccr4AfcjC713iPlGS6c40ktiydvIVrltTxAauS51y32SZnGqmPGwDZbWUYo36hihpxGJYRb/31UDbWpQVkq5J53gzAOrmIRZZQd1HHzFxYNY553rIApD6/CbvagUbN6ek7QFl6d0fNPx9dQJy0FYZSPgMosBaurRRUkDuyZSOdsJDkb6Z6N9M5G+l1I/GTR5X5BpYzjLlyOqFfdbOn4cxq/Vcbn1FTcwfo3Q4HI+ZYm7a9gMYo7yvkEzbsrrPtYdM79V/rWdXwaZ3LbWUWWCMnyT9bptwZRjbH1m2KZWHKYom9194Pxem0ar8AV+tr8YGsdowIPaNTlVpwGzeAVr8rKnM8AN5JtluQJZASVTKuGnvssAh9oSncgUVxhjxLfX6cZlyzhrOLC1oXZsOrSGbxki/z2VOTQgs/7sIgjXb4HSJf4n4BuTrCrYb5ju2fKJ5GESDC5rx9rE9HO+AfnlAFfs/U6y+d6pjpiXniBjdfbEO8rtymA+gpfuIHphDb+Xbk6JOqaodpyG5TKgZuUvuPbLglLjJ7pB8SxvE9KojlJK+PRsUucrmmFnhmUOImOm5TCdCzfI77ptxYafaFE52hkOWmHWOp48EnntVf+XxAdXPY47EXhGmTHnYLruFFPkSXNRhCsOfaiTGjnncDCF4xYwC882VK9PtGVa1hSXo82tE1H3eaP3MVGwVw8H+ktPVhqDOqv6pTia4qfH/Wmfnz8AgAA//8=";

        // the below exist just to make saving less cumbersome
        [NonSerialized]
        private IDalamudPluginInterface? PluginInterface;

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            this.PluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.PluginInterface!.SavePluginConfig(this);
        }
    }
}
