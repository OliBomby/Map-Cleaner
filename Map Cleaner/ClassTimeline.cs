using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Map_Cleaner
{
    class TimelineObject
    {
        public HitObject Origin { get; set; }
        public double Time { get; set; }
        public int Repeat { get; set; }

        public bool IsCircle { get; set; } = false;
        public bool IsSliderHead { get; set; } = false;
        public bool IsSliderRepeat { get; set; } = false;
        public bool IsSliderEnd { get; set; } = false;
        public bool IsSpinnerHead { get; set; } = false;
        public bool IsSpinnerEnd { get; set; } = false;
        public bool IsHoldnoteHead { get; set; } = false;
        public bool IsHoldnoteEnd { get; set; } = false;

        public int SampleSet { get; set; }
        public int AdditionSet { get; set; }
        public bool Normal { get; set; }
        public bool Whistle { get; set; }
        public bool Finish { get; set; }
        public bool Clap { get; set; }

        public bool HasHitsound { get; set; }

        public int CustomIndex { get; set; }
        public double SampleVolume { get; set; }
        public string Filename { get; set; }

        // Special combined with greenline
        public int FenoSampleSet { get; set; }
        public int FenoAdditionSet { get; set; }
        public int FenoCustomIndex { get; set; }
        public double FenoSampleVolume { get; set; }

        public TimelineObject(HitObject origin, double time, int objectType, int repeat, int hitsounds, int sampleset, int additionset)
        {
            Origin = origin;
            Time = time;

            BitArray b = new BitArray(new int[] { hitsounds });
            Normal = b[0];
            Whistle = b[1];
            Finish = b[2];
            Clap = b[3];

            SampleSet = sampleset;
            AdditionSet = additionset;

            BitArray c = new BitArray(new int[] { objectType });
            IsCircle = c[0];
            bool isSlider = c[1];
            bool isSpinner = c[3];
            bool isHoldNote = c[7];

            if(repeat == 0)
            {
                IsSliderHead = isSlider;
                IsSpinnerHead = isSpinner;
                IsHoldnoteHead = isHoldNote;

                if (IsCircle || isHoldNote) // Can have custom index/volume/filename
                {
                    CustomIndex = origin.CustomIndex;
                    SampleVolume = origin.SampleVolume;
                    Filename = origin.Filename;
                }
            }
            else if (repeat == origin.Repeat)
            {
                IsSliderEnd = isSlider;
                IsSpinnerEnd = isSpinner;
                IsHoldnoteEnd = isHoldNote;
            }
            else
            {
                IsSliderRepeat = isSlider;
            }
            HasHitsound = IsCircle || IsSliderHead || IsHoldnoteHead || IsSliderEnd || IsSpinnerEnd || IsSliderRepeat;

            Repeat = repeat;
        }

        public int GetHitsounds()
        {
            return GetIntFromBitArray(new BitArray(new bool[] { Normal, Whistle, Finish, Clap }));
        }

        private int GetIntFromBitArray(BitArray bitArray)
        {
            if (bitArray.Length > 32)
                throw new ArgumentException("Argument length shall be at most 32 bits.");

            int[] array = new int[1];
            bitArray.CopyTo(array, 0);
            return array[0];
        }
    }

    class Timeline
    {
        public List<TimelineObject> TimeLineObjects { get; set; }

        public Timeline(List<HitObject> hitObjects, Timing timing)
        {
            // Convert all the HitObjects to TimeLineObjects
            TimeLineObjects = new List<TimelineObject>();

            foreach(HitObject ho in hitObjects)
            {
                ho.TimelineObjects = new List<TimelineObject>();
                if (ho.IsCircle)
                {
                    TimeLineObjects.Add(new TimelineObject(ho, ho.Time, ho.ObjectType, 0, ho.Hitsounds, ho.SampleSet, ho.AdditionSet));
                    ho.TimelineObjects.Add(TimeLineObjects.Last());
                }
                else if (ho.IsSlider)
                {
                    // Adding TimeLineObject for every repeat of the slider
                    double sliderTemporalLength = timing.CalculateSliderTemporalLength(ho.Time, ho.PixelLength);

                    for(int i = 0; i <= ho.Repeat; i++)
                    {
                        double time = Math.Floor(ho.Time + sliderTemporalLength * i);
                        TimeLineObjects.Add(new TimelineObject(ho, time, ho.ObjectType, i, ho.EdgeHitsounds[i], ho.EdgeSampleSets[i], ho.EdgeAdditionSets[i]));
                        ho.TimelineObjects.Add(TimeLineObjects.Last());
                    }
                }
                else if (ho.IsSpinner) // Only the end has hitsounds
                {
                    TimeLineObjects.Add(new TimelineObject(ho, ho.Time, ho.ObjectType, 0, 0, 0, 0));
                    ho.TimelineObjects.Add(TimeLineObjects.Last());
                    TimeLineObjects.Add(new TimelineObject(ho, ho.EndTime, ho.ObjectType, 1, ho.Hitsounds, ho.SampleSet, ho.AdditionSet));
                    ho.TimelineObjects.Add(TimeLineObjects.Last());
                }
                else // Hold note. Only start has hitsounds
                {
                    TimeLineObjects.Add(new TimelineObject(ho, ho.Time, ho.ObjectType, 0, ho.Hitsounds, ho.SampleSet, ho.AdditionSet));
                    ho.TimelineObjects.Add(TimeLineObjects.Last());
                    TimeLineObjects.Add(new TimelineObject(ho, ho.EndTime, ho.ObjectType, 1, 0, 0, 0));
                    ho.TimelineObjects.Add(TimeLineObjects.Last());
                }
            }

            // Sort the TimeLineObjects by their time
            TimeLineObjects = TimeLineObjects.OrderBy(o => o.Time).ToList();
        }

        public Timeline(List<TimelineObject> timeLineObjects)
        {
            TimeLineObjects = timeLineObjects;
        }

        public Timeline GetTimeLineObjectsInRange(double start, double end)
        {
            List<TimelineObject> list = new List<TimelineObject>();
            foreach (TimelineObject tlo in TimeLineObjects)
            {
                if (tlo.Time >= start)
                {
                    if(tlo.Time < end)
                    {
                        list.Add(tlo);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            return new Timeline(list);
        }

        public void GiveTimingPoints(Timing timing)
        {
            foreach(TimelineObject tlo in TimeLineObjects)
            {
                TimingPoint tp = timing.GetTimingPointAtTime(tlo.Time + 5); // +5 for the weird offset in hitsounding greenlines
                if (tlo.SampleSet == 0)
                {
                    tlo.FenoSampleSet = tp.SampleSet;
                }
                else
                {
                    tlo.FenoSampleSet = tlo.SampleSet;
                }
                if (tlo.AdditionSet == 0)
                {
                    tlo.FenoAdditionSet = tlo.FenoSampleSet;
                }
                else
                {
                    tlo.FenoAdditionSet = tlo.AdditionSet;
                }
                if (tlo.CustomIndex == 0)
                {
                    tlo.FenoCustomIndex = tp.SampleIndex;
                }
                else
                {
                    tlo.FenoCustomIndex = tlo.CustomIndex;
                }
                if (tlo.SampleVolume  == 0)
                {
                    tlo.FenoSampleVolume = tp.Volume;
                }
                else
                {
                    tlo.FenoSampleVolume = tlo.SampleVolume;
                }
            }
        }
    }
}
