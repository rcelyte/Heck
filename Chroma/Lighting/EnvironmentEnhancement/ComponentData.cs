﻿namespace Chroma.Lighting.EnvironmentEnhancement
{
    internal interface IComponentData
    {
    }

    internal record TrackLaneRingsManagerComponentData : IComponentData
    {
        internal TrackLaneRingsManager? OldTrackLaneRingsManager { get; set; }

        internal TrackLaneRingsManager? NewTrackLaneRingsManager { get; set; }
    }
}
