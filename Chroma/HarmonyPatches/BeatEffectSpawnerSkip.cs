﻿using Heck;
using SiraUtil.Affinity;
using Zenject;

namespace Chroma.HarmonyPatches
{
    internal class BeatEffectSpawnerSkip : IAffinity
    {
        private readonly CustomData _customData;

        private BeatEffectSpawnerSkip([Inject(Id = ChromaController.ID)] CustomData customData)
        {
            _customData = customData;
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(BeatEffectSpawner), nameof(BeatEffectSpawner.HandleNoteDidStartJump))]
        private bool Prefix(NoteController noteController)
        {
            if (!_customData.Resolve(noteController.noteData, out ChromaNoteData? chromaData))
            {
                return true;
            }

            return chromaData.DisableSpawnEffect != true;
        }
    }
}
