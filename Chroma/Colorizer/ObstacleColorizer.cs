﻿using System.Collections.Generic;
using IPA.Utilities;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;

namespace Chroma.Colorizer
{
    [UsedImplicitly]
    public class ObstacleColorizerManager
    {
        private readonly ObstacleColorizer.Factory _factory;

        internal ObstacleColorizerManager(ObstacleColorizer.Factory factory)
        {
            _factory = factory;
        }

        public Dictionary<ObstacleControllerBase, ObstacleColorizer> Colorizers { get; } = new();

        public Color? GlobalColor { get; private set; }

        public ObstacleColorizer GetColorizer(ObstacleControllerBase obstactleController) => Colorizers[obstactleController];

        public void Colorize(ObstacleControllerBase obstactleController, Color? color) => GetColorizer(obstactleController).Colorize(color);

        [PublicAPI]
        public void GlobalColorize(Color? color)
        {
            GlobalColor = color;
            foreach (KeyValuePair<ObstacleControllerBase, ObstacleColorizer> valuePair in Colorizers)
            {
                valuePair.Value.Refresh();
            }
        }

        internal void Create(ObstacleControllerBase obstacleController)
        {
            Colorizers.Add(obstacleController, _factory.Create(obstacleController));
        }
    }

    [UsedImplicitly]
    public class ObstacleColorizer : ObjectColorizer
    {
        private static readonly FieldAccessor<StretchableObstacle, ParametricBoxFrameController>.Accessor _obstacleFrameAccessor = FieldAccessor<StretchableObstacle, ParametricBoxFrameController>.GetAccessor("_obstacleFrame");
        private static readonly FieldAccessor<StretchableObstacle, ParametricBoxFakeGlowController>.Accessor _obstacleFakeGlowAccessor = FieldAccessor<StretchableObstacle, ParametricBoxFakeGlowController>.GetAccessor("_obstacleFakeGlow");
        private static readonly FieldAccessor<StretchableObstacle, float>.Accessor _addColorMultiplierAccessor = FieldAccessor<StretchableObstacle, float>.GetAccessor("_addColorMultiplier");
        private static readonly FieldAccessor<StretchableObstacle, float>.Accessor _obstacleCoreLerpToWhiteFactorAccessor = FieldAccessor<StretchableObstacle, float>.GetAccessor("_obstacleCoreLerpToWhiteFactor");
        private static readonly FieldAccessor<StretchableObstacle, MaterialPropertyBlockController[]>.Accessor _materialPropertyBlockControllersAccessor = FieldAccessor<StretchableObstacle, MaterialPropertyBlockController[]>.GetAccessor("_materialPropertyBlockControllers");

        private static readonly int _tintColorID = Shader.PropertyToID("_TintColor");
        private static readonly int _addColorID = Shader.PropertyToID("_AddColor");

        private readonly ObstacleColorizerManager _manager;

        private readonly ParametricBoxFrameController _obstacleFrame;
        private readonly ParametricBoxFakeGlowController _obstacleFakeGlow;
        private readonly float _addColorMultiplier;
        private readonly float _obstacleCoreLerpToWhiteFactor;
        private readonly MaterialPropertyBlockController[] _materialPropertyBlockControllers;

        private ObstacleColorizer(
            ObstacleControllerBase obstacleController,
            ObstacleColorizerManager manager,
            ColorManager colorManager)
        {
            StretchableObstacle stretchableObstacle = obstacleController.GetComponent<StretchableObstacle>();
            _obstacleFrame = _obstacleFrameAccessor(ref stretchableObstacle);
            _obstacleFakeGlow = _obstacleFakeGlowAccessor(ref stretchableObstacle);
            _addColorMultiplier = _addColorMultiplierAccessor(ref stretchableObstacle);
            _obstacleCoreLerpToWhiteFactor = _obstacleCoreLerpToWhiteFactorAccessor(ref stretchableObstacle);
            _materialPropertyBlockControllers = _materialPropertyBlockControllersAccessor(ref stretchableObstacle);

            _manager = manager;
            OriginalColor = colorManager.obstaclesColor;
        }

        protected override Color? GlobalColorGetter => _manager.GlobalColor;

        internal override void Refresh()
        {
            Color color = Color;
            if (color == _obstacleFrame.color)
            {
                return;
            }

            _obstacleFrame.color = color;
            _obstacleFrame.Refresh();
            if (_obstacleFakeGlow != null)
            {
                _obstacleFakeGlow.color = color;
                _obstacleFakeGlow.Refresh();
            }

            Color value = color * _addColorMultiplier;
            value.a = 0f;
            foreach (MaterialPropertyBlockController materialPropertyBlockController in _materialPropertyBlockControllers)
            {
                materialPropertyBlockController.materialPropertyBlock.SetColor(_addColorID, value);
                materialPropertyBlockController.materialPropertyBlock.SetColor(_tintColorID, Color.Lerp(color, Color.white, _obstacleCoreLerpToWhiteFactor));
                materialPropertyBlockController.ApplyChanges();
            }
        }

        [UsedImplicitly]
        internal class Factory : PlaceholderFactory<ObstacleControllerBase, ObstacleColorizer>
        {
        }
    }
}
