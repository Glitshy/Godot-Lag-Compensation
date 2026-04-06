using Godot;
using PG.LagCompensation.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PG.LagCompensation.Base.TrackerLayeredBase;

namespace PG.LagCompensation.Base
{

    /// <summary>
    /// Abstract clss for all trackers (including layer), can store position/oration/layer data and perform bounding sphere parametric raycasts as an initial hit check before using parametic or physical raycasts against the actual shape
    /// </summary>
    [GlobalClass]
    public abstract partial class TrackerLayeredBase : TrackerBase
    {
        /// <summary>
        /// Determines how interpolation behaves between two states in the buffer. 
        /// Typically does linear interpolation for the position/rotation, while for the layer either the newer or older value is chosen.
        /// </summary>
        public enum InterpolationMode
        {
            /// <summary>
            /// 'Balanced' --> layers change based on whether interpolation factor 't' is greater or smaller than 0.
            /// <br></br>
            /// position/rotation is always linearily interpolated based on 't'
            /// </summary>
            Balanced = 0,
            /// <summary>
            /// 'OlderBiased' --> layers are always based on the older value.
            /// <br></br>
            /// position/rotation is always linearily interpolated based on 't'
            /// </summary>
            OlderBiased = 1,
            /// <summary>
            /// 'OlderBiased' --> layers are always based on the older value.
            /// <br></br>
            /// 'Cutoff' --> if layers change between older and newer, the position/rotation are based on the older value, independent of 't'. If layers are identical, does regular linear interpolation.
            /// </summary>
            OlderBiasedCutoff = 2,
            /// <summary>
            /// 'NewerBiased' --> layers are always based on the newer value.
            /// <br></br>
            /// position/rotation is always linearily interpolated based on 't'
            /// </summary>
            NewerBiased = 11,
            /// <summary>
            /// 'NewerBiased' --> layers are always based on the newer value.
            /// <br></br>
            /// 'Cutoff' --> if layers change between older and newer, the position/rotation are based on the older value, independent of 't'. If layers are identical, does regular linear interpolation.
            /// </summary>
            NewerBiasedCutoff = 12
        }

        /// <summary>
        /// In addition to the buffers in the base <see cref="TrackerBase"/> class, this buffer contains a layer mask. 
        /// Therefore, the InterpolateAndCacheAtIndex() and AddFrame() methods are overridden.
        /// </summary>
        protected RingBuffer<uint> _bufferLayers;

        /// <summary>
        /// Assigned by <see cref="InterpolateAndCacheAtIndex"/>.
        /// Layers corresponding to time <c>_cachedTime</c> and position/rotation <c>_cachedPosRot</c>
        /// </summary>
        protected uint _cachedLayers;

        public uint GetCachedLayers => _cachedLayers;

        protected abstract uint GetLayers { get; }


        public abstract InterpolationMode interpolationMode { get; set; }

        #region Lag Compensation

        // override base in order to also store layer data
        public override void AddFrame(double time)
        {
            // base still handles time and position/rotation
            base.AddFrame(time);

            // initialize buffer if it hasn't happened yet
            if (_bufferLayers == null)
            {
                _bufferLayers = new RingBuffer<uint>(GetHistoryLength);
            }

            _bufferLayers.Add(GetLayers);
        }

        // override base in order to also interpolate layer data
        protected override void InterpolateAndCacheAtIndex(int olderIndex, double t)
        {
            // new transform and layer logic:
            // if there is a newer stored frame, use that
            // there is no newer stored frame --> use the current layer

            TransformFrameData oldTransform = _bufferTransform[olderIndex];
            // Note: getting current transform and rotation is more performance intensive than cached frame data
            TransformFrameData newTransform = (olderIndex < _bufferTransform.Count - 1) ? _bufferTransform[olderIndex + 1] : new TransformFrameData(GetTargetNode);

            bool oldNewLayersMismatch;

            uint oldLayers = _bufferLayers[olderIndex];
            uint newLayers = (olderIndex < _bufferLayers.Count - 1) ? _bufferLayers[olderIndex + 1] : GetLayers;

            oldNewLayersMismatch = oldLayers != newLayers;

            if (oldNewLayersMismatch)
            {
                switch (interpolationMode)
                {
                    case InterpolationMode.Balanced:
                        _cachedLayers = t > 0.5 ? newLayers : oldLayers;
                        break;
                    case InterpolationMode.OlderBiased:
                        _cachedLayers = oldLayers;
                        break;
                    case InterpolationMode.OlderBiasedCutoff:
                        _cachedLayers = oldLayers;
                        _cachedPosRot = oldTransform;
                        return;
                    case InterpolationMode.NewerBiased:
                        _cachedLayers = newLayers;
                        break;
                    case InterpolationMode.NewerBiasedCutoff:
                        _cachedLayers = newLayers;
                        _cachedPosRot = newTransform;
                        return;
                }
            }
            else
            {
                _cachedLayers = oldLayers;
            }

            // default for all other cases which do not return early --> use linear interpolation
            _cachedPosRot = TransformFrameData.Interpolate(oldTransform, newTransform, t);
        }

        public override void InitializeBuffers()
        {
            base.InitializeBuffers();

            _bufferLayers = new RingBuffer<uint>(GetHistoryLength);
        }

        #endregion
    }
}
