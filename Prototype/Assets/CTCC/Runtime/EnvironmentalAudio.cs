using System;
using UnityEngine;

namespace CTCC
{
    [Serializable]
    public sealed class RoomLoop
    {
        public AudioSource source;
        [Range(0,1)] public float level;
        public bool report;
    }

    public sealed class EnvironmentalAudio : MonoBehaviour
    {
        public RoomLoop[] loops;
        public AudioSource propCue;
        public AudioClip paper, stamp, slippers, step, switchLayer;
        public bool ReportLayer { get; set; }
        private bool mono;
        private float footstepPitch;

        private void Start()
        {
            foreach(var loop in loops)
            {
                loop.source.volume=0;
                loop.source.Play();
            }
        }

        private void Update()
        {
            float amount=Time.unscaledDeltaTime*1.55f;
            foreach(var loop in loops)
            {
                var source=loop.source;
                float target=ReportLayer==loop.report ? loop.level : 0;
                source.volume=Mathf.MoveTowards(source.volume,target,amount);
            }
        }

        public void Apply(VisitSettings options)
        {
            AudioListener.volume=Mathf.Clamp01(options.volume);
            mono=options.monoAudio;
            foreach(var loop in loops)
                loop.source.spatialBlend=mono ? 0 : .96f;
        }

        public void From(string cue,Vector3 at)
        {
            var sound=cue switch
            {
                "paper"=>paper, "stamp"=>stamp, "slippers"=>slippers, "switch"=>switchLayer, _=>null
            };
            if (sound==null) return;
            propCue.transform.position=at;
            propCue.spatialBlend=mono?0:.92f;
            propCue.PlayOneShot(sound,cue=="switch"?.38f:.58f);
        }

        public void Step(AudioSource source,bool running,bool crouched)
        {
            if(step==null) return;
            footstepPitch=Mathf.Repeat(footstepPitch+.618f,1);
            source.pitch=.92f+footstepPitch*.17f;
            source.PlayOneShot(step,crouched?.15f:running?.49f:.31f);
        }
    }
}
