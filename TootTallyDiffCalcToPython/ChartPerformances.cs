using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TootTallyDiffCalcTTV2
{
    public struct ChartPerformances
    {
        public static readonly float[] weights = {
             1.0000f, 0.9000f, 0.8100f, 0.7290f, 0.6561f, 0.5905f, 0.5314f, 0.4783f,
             0.4305f, 0.3874f, 0.3487f, 0.3138f, 0.2824f, 0.2542f, 0.2288f, 0.2059f,
             0.1853f, 0.1668f, 0.1501f, 0.1351f, 0.1216f, 0.1094f, 0.0985f, 0.0887f,
             0.0798f, 0.0718f, 0.0646f, 0.0582f, 0.0524f, 0.0472f, 0.0425f, 0.0383f,
             0.0345f, 0.0311f, 0.0280f, 0.0252f, 0.0227f, 0.0204f, 0.0184f, 0.0166f,
             0.0149f, 0.0134f, 0.0121f, 0.0109f, 0.0098f, 0.0088f, 0.0079f, 0.0071f,
             0.0064f, 0.0057f, 0.0051f, 0.0046f, 0.0041f, 0.0037f, 0.0033f, 0.0030f,
             0.0027f, 0.0024f, 0.0022f, 0.0020f, 0.0018f, 0.0016f, 0.0015f, 0.0013f // :)
        };
        public DataVector[][] aimPerfMatrix;
        public DataVectorAnalytics[] aimAnalyticsDict;

        public DataVector[][] tapPerfMatrix;
        public DataVectorAnalytics[] tapAnalyticsDict;

        public float[] aimRatingDict;
        public float[] tapRatingDict;
        public float[] starRatingDict;

        private readonly float tempo;
        private readonly int ALL_NOTE_COUNT;
        private readonly int noteCount;
        private readonly Note[] _notesArray;
        private const float PLAY_AREA_RANGE = 360;

        public ChartPerformances(Chart chart)
        {
            _notesArray = chart.notesArray;
            ALL_NOTE_COUNT = chart.notesArray.Length;
            noteCount = chart.noteCount;
            tempo = chart.tempo;

            aimPerfMatrix = new DataVector[Utils.GAME_SPEED.Length][];
            aimAnalyticsDict = new DataVectorAnalytics[Utils.GAME_SPEED.Length];

            tapPerfMatrix = new DataVector[Utils.GAME_SPEED.Length][];
            tapAnalyticsDict = new DataVectorAnalytics[Utils.GAME_SPEED.Length];

            aimRatingDict = new float[Utils.GAME_SPEED.Length];
            tapRatingDict = new float[Utils.GAME_SPEED.Length];
            starRatingDict = new float[Utils.GAME_SPEED.Length];

            for (int i = 0; i < Utils.GAME_SPEED.Length; i++)
            {
                aimPerfMatrix[i] = new DataVector[ALL_NOTE_COUNT];
                tapPerfMatrix[i] = new DataVector[ALL_NOTE_COUNT];
            }
        }

        public const float AIM_DIV = 14;
        public const float TAP_DIV = 14;
        public const float ACC_DIV = 10;
        public const float MAX_DIST = 5f;
        public const int MAX_NOTE_COUNT = 16;
        public const float CHEESABLE_THRESHOLD = 34.375f;

        public void CalculatePerformance(int speedIndex)
        {
            var newTempo = Utils.GAME_SPEED[speedIndex] * tempo;
            float aimEnd = 0, aimSta = 0, tapEnd = 0, tapSta = 0;
            for (int i = 1; i < ALL_NOTE_COUNT; i++) //Main Forward Loop
            {
                int noteCount = 0;
                float aimStrain = 0, tapStrain = 0;
                float weightSum = 1;
                ConvertNote(in _notesArray[i], tempo, newTempo, out var n1Current);
                ConvertNote(in _notesArray[i - 1], tempo, newTempo, out var n2Prev);
                Note n1Prev = default;
                n1Prev.count = -1;
                for (int j = i - 1; j >= 0 && noteCount < MAX_NOTE_COUNT && (MathF.Abs(n1Current.position - n2Prev.position) <= MAX_DIST || i - j <= 2); j--) //Secondary Backward Loop
                {
                    ConvertNote(in _notesArray[j], tempo, newTempo, out n2Prev);
                    ConvertNote(in _notesArray[j + 1], tempo, newTempo, out var n2Next);
                    noteCount++;
                    var weight = weights[noteCount * 2];
                    if (n2Prev.position >= n2Next.position) break;
                    var lengthSum = n2Prev.length;
                    var slideCount = 0f;
                    var slideVelocity = 0f;
                    var flatLength = 0f;
                    if (n2Prev.pitchDelta != 0)
                    {
                        slideCount++;
                        var pitchDelta = MathF.Abs(n2Prev.pitchDelta);
                        var deltaSlide = MathF.Sqrt(NormalizePitch(pitchDelta)) * (pitchDelta >= CHEESABLE_THRESHOLD ? .45f : .1f);
                        slideVelocity += deltaSlide / MathF.Pow(n2Prev.length, 1.38f);
                    }
                    else
                        flatLength += n2Prev.length * .2f;
                    while (n2Prev.isSlider) //Merge all sliders into one note
                    {
                        if (j-- <= 0)
                            break;
                        ConvertNote(in _notesArray[j], tempo, newTempo, out n2Prev);
                        ConvertNote(in _notesArray[j + 1], tempo, newTempo, out n2Next);

                        lengthSum += n2Prev.length;
                        if (MathF.Abs(n2Prev.pitchDelta) >= CHEESABLE_THRESHOLD / 10f)
                        {
                            slideCount++;
                            var pitchDelta = MathF.Abs(n2Prev.pitchDelta);
                            var deltaSlide = MathF.Sqrt(NormalizePitch(pitchDelta)) * (pitchDelta >= CHEESABLE_THRESHOLD ? .75f : .1f);
                            slideVelocity += deltaSlide / MathF.Pow(n2Prev.length, 1.38f);
                        }
                        else
                            flatLength += n2Prev.length * .2f;
                    }

                    if (n1Prev.count == -1)
                        n1Prev = n2Prev;

                    //Slide
                    if (slideCount != 0)
                    {
                        slideVelocity /= slideCount;
                        aimStrain += (slideVelocity * weight) / ACC_DIV;
                        aimStrain *= (lengthSum - flatLength) / lengthSum;
                    }

                    //Aim
                    var deltaTime = n2Next.position - n2Prev.position;
                    var aimDistance = MathF.Abs(NormalizePitch(n2Next.pitchStart - n2Prev.pitchEnd));
                    if (aimDistance != 0)
                    {
                        var currVelocity = (MathF.Sqrt(aimDistance + .02f) * .95f) / MathF.Pow(deltaTime, 1.32f);
                        aimStrain += (currVelocity * weight) / AIM_DIV;
                    }

                    //Tap
                    var baseValue = (MathF.Sqrt(aimDistance) / 10f) + .0725f;
                    tapStrain += ((baseValue / MathF.Pow(deltaTime, 1.36f)) * weight) / TAP_DIV;
                    weightSum += weight;
                }

                var tapDelta = MathF.Sqrt(n1Current.position - n1Prev.position);

                tapSta = ComputeStamina(tapStrain * 1.85f, tapSta, tapDelta, STA_DIV);
                tapEnd = ComputeEndurance(tapSta * 1.55f, tapEnd, tapDelta, END_DIV);

                aimSta = ComputeStamina(aimStrain * .55f, aimSta, tapDelta, STA_DIV);
                aimEnd = ComputeEndurance(aimSta * 1.55f, aimEnd, tapDelta, END_DIV);

                aimPerfMatrix[speedIndex][i] = new DataVector(n1Current.position, aimStrain, aimSta, aimEnd, weightSum);
                tapPerfMatrix[speedIndex][i] = new DataVector(n1Current.position, tapStrain, tapSta, tapEnd, weightSum);
            }
        }

        //https://www.desmos.com/calculator/ylbxpzlkzy Old
        //https://www.desmos.com/calculator/k6uxgkyhgq Curr
        //public static float ComputeStaminaMult(float decayRate) => MathF.Pow(MathF.E, -MathF.Pow(decayRate * .6f, 1.5f));
        //public static float ComputeEnduranceAimMult(float decayRate) => MathF.Pow(MathF.E, -MathF.Pow(decayRate * .03f, 2f));
        //public static float ComputeEnduranceTapMult(float decayRate) => MathF.Pow(MathF.E, -decayRate / 10f);
        //public static float ComputeStaminaAimMult(float decayRate) => MathF.Pow(MathF.E, -MathF.Pow(decayRate * .025f, 1.5f));
        //public static float ComputeStaminaTapMult(float decayRate) => MathF.Pow(MathF.E, -decayRate * 1.5f);

        const float STA_RISE_RATE = 1.45f;
        const float STA_DECAY_RATE = .25f;
        const float STA_DIV = 5f;
        const float END_RISE_RATE = .15f;
        const float END_DECAY_RATE = .15f;
        const float END_DIV = 25f;

        public static float ComputeStamina(float strain, float stamina, float tapDelta, float div)
        {
            return stamina + ((strain - stamina) / div) * ((strain > stamina) ?
                                              1f - MathF.Pow(MathF.E, -STA_RISE_RATE * tapDelta) :
                                              1f - MathF.Pow(MathF.E, -STA_DECAY_RATE * tapDelta));
            //return newStam < 0 ? 0 : newStam;
        }
        public static float ComputeEndurance(float stamina, float endurance, float tapDelta, float div)
        {
            return endurance + ((stamina - endurance) / div) * ((stamina > endurance) ?
                                              1f - MathF.Pow(MathF.E, -END_RISE_RATE * tapDelta) :
                                              1f - MathF.Pow(MathF.E, -END_DECAY_RATE * tapDelta));
            //return newEnd < 0 ? 0 : newEnd;
        }

        public static void ConvertNote(in Note note, float tempo, float newTempo, out Note n)
        {
            n = note;
            n.position = (note.position * tempo) / newTempo;
            n.length = (note.length * tempo) / newTempo;
        }

        public static float NormalizePitch(float pitch) => pitch / PLAY_AREA_RANGE;

        public void CalculateAnalytics(int gamespeed)
        {
            aimAnalyticsDict[gamespeed] = new DataVectorAnalytics(aimPerfMatrix[gamespeed]);
            tapAnalyticsDict[gamespeed] = new DataVectorAnalytics(tapPerfMatrix[gamespeed]);
        }


        public void CalculateRatings(int gamespeed)
        {
            var aimRating = aimRatingDict[gamespeed] = aimAnalyticsDict[gamespeed].perfWeightedAverage + 0.01f;
            var tapRating = tapRatingDict[gamespeed] = tapAnalyticsDict[gamespeed].perfWeightedAverage + 0.01f;

            if (aimRating != 0 && tapRating != 0)
            {
                var totalRating = aimRating + tapRating;
                var aimPerc = aimRating / totalRating;
                var tapPerc = tapRating / totalRating;
                var aimWeight = (aimPerc + BIAS) * AIM_WEIGHT;
                var tapWeight = (tapPerc + BIAS) * TAP_WEIGHT;
                var totalWeight = aimWeight + tapWeight;
                starRatingDict[gamespeed] = ((aimRating * aimWeight) + (tapRating * tapWeight)) / totalWeight;
            }
            else
                starRatingDict[gamespeed] = 0f;
        }

        public float GetDynamicAimRating(int hitCount, float speed) => GetDynamicSkillRating(hitCount, speed, aimPerfMatrix);
        public float GetDynamicTapRating(int hitCount, float speed) => GetDynamicSkillRating(hitCount, speed, tapPerfMatrix);

        public float GetDynamicAimTT(int hitCount, float speed) => GetDynamicTTRating(hitCount, speed, aimPerfMatrix);
        public float GetDynamicTapTT(int hitCount, float speed) => GetDynamicTTRating(hitCount, speed, tapPerfMatrix);

        private float GetDynamicSkillRating(int hitCount, float speed, DataVector[][] skillRatingMatrix)
        {
            var index = (int)((speed - 0.5f) / .25f);

            if (skillRatingMatrix[index].Length <= 1 || hitCount <= 0)
                return 0;
            else if (speed % .25f == 0)
                return CalcSkillRating(hitCount, skillRatingMatrix[index]);

            var r1 = CalcSkillRating(hitCount, skillRatingMatrix[index]);
            var r2 = CalcSkillRating(hitCount, skillRatingMatrix[index + 1]);

            var minSpeed = Utils.GAME_SPEED[index];
            var maxSpeed = Utils.GAME_SPEED[index + 1];
            var by = (speed - minSpeed) / (maxSpeed - minSpeed);
            return Utils.Lerp(r1, r2, by);
        }

        private float GetDynamicTTRating(int hitCount, float speed, DataVector[][] skillRatingMatrix)
        {
            var index = (int)((speed - 0.5f) / .25f);

            if (skillRatingMatrix[index].Length <= 1 || hitCount <= 0)
                return 0;
            else if (speed % .25f == 0)
                return CalcTTRating(hitCount, skillRatingMatrix[index]);

            var r1 = CalcTTRating(hitCount, skillRatingMatrix[index]);
            var r2 = CalcTTRating(hitCount, skillRatingMatrix[index + 1]);

            var minSpeed = Utils.GAME_SPEED[index];
            var maxSpeed = Utils.GAME_SPEED[index + 1];
            var by = (speed - minSpeed) / (maxSpeed - minSpeed);
            return Utils.Lerp(r1, r2, by);
        }

        public const float MAP = .05f;
        public const float MACC = .5f;

        private float CalcSkillRating(int hitCount, DataVector[] skillRatingArray)
        {
            int maxRange;

            float percent = 1f;
            if (hitCount < noteCount)
                percent = MathF.Min((float)hitCount / noteCount, 1f);

            if (percent <= MACC)
                maxRange = (int)Math.Clamp(skillRatingArray.Length * (percent * (MAP / MACC)), 1, skillRatingArray.Length);
            else
                maxRange = (int)Math.Clamp(skillRatingArray.Length * ((percent - MACC) * ((1f - MAP) / (1f - MACC)) + MAP), 1, skillRatingArray.Length);

            var array = skillRatingArray.OrderBy(x => x.strain + x.stamina + x.endurance).ToArray()[0..maxRange];
            var analytics = new DataVectorAnalytics(array);
            return analytics.perfWeightedAverage + .01f;
        }

        private float CalcTTRating(int hitCount, DataVector[] skillRatingArray)
        {
            int maxRange;

            float percent = 1f;
            if (hitCount < noteCount)
                percent = MathF.Min((float)hitCount / noteCount, 1f);

            if (percent <= MACC)
                maxRange = (int)Math.Clamp(skillRatingArray.Length * (percent * (MAP / MACC)), 1, skillRatingArray.Length);
            else
                maxRange = (int)Math.Clamp(skillRatingArray.Length * ((percent - MACC) * ((1f - MAP) / (1f - MACC)) + MAP), 1, skillRatingArray.Length);

            var array = skillRatingArray.OrderBy(x => x.strain + x.stamina + x.endurance).ToArray()[0..maxRange];
            var analytics = new DataVectorAnalytics(array);
            return analytics.sumTT + .01f;
        }

        public const float AIM_WEIGHT = 1.25f;
        public const float TAP_WEIGHT = 1f;
        public const float BIAS = .5f;

        public static readonly float[] HDWeights = { .11f, .09f };
        public static readonly float[] FLWeights = { .12f, .1f };
        public static readonly float[] EZWeights = { -.48f, -.25f };

        public float GetDynamicDiffRating(int hitCount, float gamespeed, string[] modifiers = null)
        {
            var aimRating = GetDynamicAimRating(hitCount, gamespeed);
            var tapRating = GetDynamicTapRating(hitCount, gamespeed);

            if (aimRating == 0 && tapRating == 0) return 0f;

            if (modifiers != null)
            {
                var aimPow = 1f;
                var tapPow = 1f;
                var isEZModeOn = modifiers.Contains("EZ");
                var mult = isEZModeOn ? .25f : 1f;
                if (modifiers.Contains("HD"))
                {
                    aimPow += HDWeights[0] * mult;
                    tapPow += HDWeights[1] * mult;
                }
                if (modifiers.Contains("FL"))
                {
                    aimPow += FLWeights[0] * mult;
                    tapPow += FLWeights[1] * mult;
                }
                if (isEZModeOn)
                {
                    aimPow += EZWeights[0];
                    tapPow += EZWeights[1];
                }
                if (modifiers.Contains("AP"))
                    aimPow = 0;
                if (modifiers.Contains("RX"))
                    tapPow = 0;

                if (aimPow < 0) aimPow = .01f;
                if (tapPow < 0) tapPow = .01f;



                aimRating *= aimPow;
                tapRating *= tapPow;
            }

            var totalRating = aimRating + tapRating;
            if (totalRating <= 0) return 0;
            var aimPerc = aimRating / totalRating;
            var tapPerc = tapRating / totalRating;
            var aimWeight = (aimPerc + BIAS) * AIM_WEIGHT;
            var tapWeight = (tapPerc + BIAS) * TAP_WEIGHT;
            var totalWeight = aimWeight + tapWeight;

            return ((aimRating * aimWeight) + (tapRating * tapWeight)) / totalWeight;
        }

        public float GetDynamicTTRating(int hitCount, float gamespeed, float multiplier, string[] modifiers = null)
        {
            var aimTT = GetDynamicAimTT(hitCount, gamespeed);
            var tapTT = GetDynamicTapTT(hitCount, gamespeed);

            if (aimTT == 0 && tapTT == 0) return 0f;

            var aimPow = 1f;
            var tapPow = 1f;
            if (modifiers != null)
            {
                var isEZModeOn = modifiers.Contains("EZ");
                var mult = isEZModeOn ? .25f : 1f;
                if (modifiers.Contains("HD"))
                {
                    aimPow += HDWeights[0] * mult;
                    tapPow += HDWeights[1] * mult;
                }
                if (modifiers.Contains("FL"))
                {
                    aimPow += FLWeights[0] * mult;
                    tapPow += FLWeights[1] * mult;
                }
                if (isEZModeOn)
                {
                    aimPow += EZWeights[0];
                    tapPow += EZWeights[1];
                }

                if (modifiers.Contains("AP"))
                    aimPow = 0;
                if (modifiers.Contains("RX"))
                    tapPow = 0;

                if (aimPow < 0) aimPow = .01f;
                if (tapPow < 0) tapPow = .01f;



                aimTT *= aimPow;
                tapTT *= tapPow;
            }

            var totalRating = aimTT + tapTT;
            if (totalRating <= 0) return 0;
            var aimPerc = aimTT / totalRating;
            var tapPerc = tapTT / totalRating;
            var aimWeight = (aimPerc + BIAS) * AIM_WEIGHT;
            var tapWeight = (tapPerc + BIAS) * TAP_WEIGHT;
            var totalWeight = aimWeight + tapWeight;

            return multiplier * ((aimTT * aimWeight) + (tapTT * tapWeight)) / totalWeight;
        }

        public void Dispose()
        {
            aimPerfMatrix = null;
            aimAnalyticsDict = null;
            aimRatingDict = null;
            tapPerfMatrix = null;
            tapAnalyticsDict = null;
            tapRatingDict = null;
            starRatingDict = null;
        }

        public struct DataVector(float time, float strain, float stamina, float endurance, float weight)
        {
            public float time = time;
            public float stamina = stamina;
            public float endurance = endurance;
            public float strain = strain;
            public float weight = weight;
        }

        public struct DataVectorAnalytics
        {
            public float perfMax, perfSum, perfWeightedAverage;
            public float weightSum;
            public float sumTT;

            public DataVectorAnalytics(DataVector[] dataVectorList)
            {
                perfMax = perfSum = perfWeightedAverage = 0;
                weightSum = 1;
                sumTT = 0;

                if (dataVectorList == null || dataVectorList.Length <= 0) return;
                CalculateWeightSum(dataVectorList);
                CalculateData(dataVectorList);
            }

            public void CalculateWeightSum(DataVector[] dataVectorList)
            {
                for (int i = 0; i < dataVectorList.Length; i++)
                    weightSum += dataVectorList[i].weight;
            }

            public void CalculateData(DataVector[] dataVectorList)
            {
                for (int i = 0; i < dataVectorList.Length; i++)
                {
                    var weight = dataVectorList[i].weight / weightSum;
                    var perf = dataVectorList[i].strain + dataVectorList[i].stamina + dataVectorList[i].endurance;
                    if (perfMax < perf)
                        perfMax = perf;
                    perfSum += perf * weight;
                    sumTT += CalcStrainTT(dataVectorList[i].strain * weight) + CalcStamTT(dataVectorList[i].stamina * weight) + CalcEnduTT(dataVectorList[i].endurance * weight);
                }
                perfWeightedAverage = perfSum;
            }
        }
        public static float CalcStrainTT(float performance) => performance * 350f;
        public static float CalcStamTT(float stamina) => stamina * 425f;
        public static float CalcEnduTT(float endurance) => endurance * 425f;

        public static float BeatToSeconds2(float beat, float bpm) => 60f / bpm * beat;
    }
}
