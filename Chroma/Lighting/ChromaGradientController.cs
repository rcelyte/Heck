﻿using System.Collections.Generic;
using Chroma.Colorizer;
using Heck.Animation;
using IPA.Utilities;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;

namespace Chroma.Lighting
{
    [UsedImplicitly]
    internal class ChromaGradientController : ITickable
    {
        private readonly LightColorizerManager _manager;
        private readonly IBpmController _bpmController;
        private readonly ChromaGradientEvent.Factory _factory;

        private ChromaGradientController(
            LightColorizerManager manager,
            IBpmController bpmController,
            ChromaGradientEvent.Factory factory)
        {
            _manager = manager;
            _bpmController = bpmController;
            _factory = factory;
        }

        private IDictionary<BasicBeatmapEventType, ChromaGradientEvent> Gradients { get; } = new Dictionary<BasicBeatmapEventType, ChromaGradientEvent>();

        public void Tick()
        {
            foreach ((BasicBeatmapEventType eventType, ChromaGradientEvent value) in new Dictionary<BasicBeatmapEventType, ChromaGradientEvent>(Gradients))
            {
                Color color = value.Interpolate();
                _manager.Colorize(eventType, true, color, color, color, color);
            }
        }

        internal bool IsGradientActive(BasicBeatmapEventType eventType)
        {
            return Gradients.ContainsKey(eventType);
        }

        internal void CancelGradient(BasicBeatmapEventType eventType)
        {
            Gradients.Remove(eventType);
        }

        internal Color AddGradient(ChromaEventData.GradientObjectData gradientObject, BasicBeatmapEventType id, float time)
        {
            CancelGradient(id);

            float duration = gradientObject.Duration;
            Color initcolor = gradientObject.StartColor;
            Color endcolor = gradientObject.EndColor;
            Functions easing = gradientObject.Easing;

            ChromaGradientEvent gradientEvent = _factory.Create(initcolor, endcolor, time, 60 * duration / _bpmController.currentBpm, id, easing);
            Gradients[id] = gradientEvent;
            return gradientEvent.Interpolate();
        }

        [UsedImplicitly]
        internal class ChromaGradientEvent
        {
            private readonly IAudioTimeSource _timeSource;
            private readonly ChromaGradientController _gradientController;
            private readonly Color _initcolor;
            private readonly Color _endcolor;
            private readonly float _start;
            private readonly float _duration;
            private readonly BasicBeatmapEventType _event;
            private readonly Functions _easing;

            internal ChromaGradientEvent(
                IAudioTimeSource timeSource,
                ChromaGradientController gradientController,
                Color initcolor,
                Color endcolor,
                float start,
                float duration,
                BasicBeatmapEventType eventType,
                Functions easing = Functions.easeLinear)
            {
                _timeSource = timeSource;
                _gradientController = gradientController;
                _initcolor = initcolor;
                _endcolor = endcolor;
                _start = start;
                _duration = duration;
                _event = eventType;
                _easing = easing;
            }

            internal Color Interpolate()
            {
                float normalTime = _timeSource.songTime - _start;
                if (normalTime < 0)
                {
                    return _initcolor;
                }

                if (normalTime <= _duration)
                {
                    return Color.LerpUnclamped(_initcolor, _endcolor, Easings.Interpolate(normalTime / _duration, _easing));
                }

                _gradientController.Gradients.Remove(_event);
                return _endcolor;
            }

            [UsedImplicitly]
            internal class Factory : PlaceholderFactory<Color, Color, float, float, BasicBeatmapEventType, Functions, ChromaGradientEvent>
            {
            }
        }
    }
}
