using System.Collections.Generic;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace TimelineInject
{
    public static class KimodoTimelineOffsetUtility
    {
        // Connected generation bakes each clip from its own root origin, and finalize resets clip
        // offsets to zero, so every clip after the first restarts at the track origin. Apply
        // Timeline's "Match Offsets To Previous Clip" so playback keeps the root continuous.
        public static bool MatchClipsToPrevious(IReadOnlyList<TimelineClip> orderedClips, out string message)
        {
            message = string.Empty;
            if (orderedClips == null || orderedClips.Count < 2) return true;

            var track = orderedClips[0].GetParentTrack() as AnimationTrack;
            PlayableDirector director = FindDirector(track);
            if (director == null)
            {
                message = "No PlayableDirector plays this Timeline; clip offsets were not matched.";
                return false;
            }

            var bound = director.GetGenericBinding(track);
            Transform matchPoint = bound is Animator animator ? animator.transform
                : bound is GameObject gameObject ? gameObject.transform
                : null;
            if (matchPoint == null)
            {
                message = "The Kimodo track has no bound Animator; clip offsets were not matched.";
                return false;
            }

            double cachedTime = director.time;
            for (int i = 1; i < orderedClips.Count; i++)
            {
                TimelineAnimationUtilities.MatchPrevious(orderedClips[i], matchPoint, director);
            }
            director.time = cachedTime;
            director.RebuildGraph();
            director.Evaluate();
            TimelineEditor.Refresh(RefreshReason.ContentsModified);
            return true;
        }

        private static PlayableDirector FindDirector(AnimationTrack track)
        {
            if (track == null) return null;
            TimelineAsset timeline = track.timelineAsset;
            PlayableDirector inspected = TimelineEditor.inspectedDirector;
            if (inspected != null && inspected.playableAsset == timeline) return inspected;

            foreach (var director in Object.FindObjectsByType<PlayableDirector>(FindObjectsInactive.Exclude))
            {
                if (director.playableAsset == timeline) return director;
            }
            return null;
        }
    }
}
