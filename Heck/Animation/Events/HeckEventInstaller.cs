﻿using JetBrains.Annotations;
using Zenject;

namespace Heck.Animation.Events
{
    [UsedImplicitly]
    internal class HeckEventInstaller : Installer
    {
        public override void InstallBindings()
        {
            if (!HeckController.FeaturesPatcher.Enabled)
            {
                return;
            }

            Container.Bind<EventController>().FromNewComponentOnRoot().AsSingle().NonLazy();
            Container.Bind<CoroutineEventManager>().AsSingle();
        }
    }
}
