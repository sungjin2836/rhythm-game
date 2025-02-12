using System;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.XR;

namespace BMS
{
    public class BMSMainDataParser : ChartDecoder
    {
        #region Random Statements
        bool isRandom = false;
        bool isIfStatementTrue = false;
        bool isCheckIfstatementStarted = false;
        int randomResult = 0;
        #endregion
        private Pattern pattern = new Pattern();
        public Pattern Pattern { get { return pattern; }}

        public BMSMainDataParser(string path) : base(path)
        {
            parseData += ParseMainData;
            ReadFile();
            // 패턴 파싱을 다하고나서 노트 bar와 beat을 기준으로 정렬
            pattern.SortObjects();
            pattern.CalculateTiming(TrackInfo.bpm);
        }

        public BMSMainDataParser(TrackInfo trackInfo): base(trackInfo)
        {
            parseData += ParseMainData;
            ReadFile();
            // 패턴 파싱을 다하고나서 노트 bar와 beat을 기준으로 정렬
            pattern.SortObjects();
            pattern.CalculateTiming(TrackInfo.bpm);
        }

        private void ParseMainData(string line)
        {
            // 메인 데이터 형식 (#001XX:AABBCC)이 아닐경우 파싱 생략
            if (line == "" || line.IndexOf(" ") > -1)
            {
                return;
            }

            string statementDataKey = line.IndexOf(" ") > -1 && line.StartsWith("#") ? line.Substring(0, line.IndexOf(" ")) : line;
            string statementDataValue = line.IndexOf(" ") > -1 && line.StartsWith("#") ? line.Substring(line.IndexOf(" ") + 1) : "";

            string mainDataKey = line.IndexOf(":") > -1 && line.StartsWith("#") ? line.Substring(1, line.IndexOf(":") - 1) : "";
            string mainDataValue = line.IndexOf(":") > -1 && line.StartsWith("#") ? line.Substring(line.IndexOf(":") + 1) : "";

            // Random에 대한 전처리 (#RANDOM 숫자 / #ENDRANDOM으로 구분) //
            // 랜덤 값 지정
            if (statementDataKey == "#RANDOM")
            {
                isRandom = true;
                Int32.TryParse(statementDataValue, out int randomNumber);
                randomResult = new System.Random().Next(1, randomNumber + 1);
            }

            // 랜덤 탈출
            if (statementDataKey == "#ENDRANDOM")
            {
                isRandom = false;
                randomResult = 0;
            }

            // 조건에 대한 전처리 (#IF 숫자 / #ENDIF로 구분) // 
            // 조건 검색
            if (statementDataKey == "#IF" && Int32.TryParse(statementDataValue, out int parsedStatementDataValue) && isRandom == true)
            {
                isCheckIfstatementStarted = true;
                if (parsedStatementDataValue == randomResult)
                {
                    isIfStatementTrue = true;
                }
                else
                {
                    isIfStatementTrue = false;
                }
            }

            // 조건 탈출
            if (statementDataKey == "#ENDIF")
            {
                isCheckIfstatementStarted = false;
                isIfStatementTrue = false;
            }

            if (isRandom == false || (isIfStatementTrue == true && isCheckIfstatementStarted == true) || isCheckIfstatementStarted == false)
            {
                if (mainDataKey != "" && mainDataValue != "")
                {
                    // 마디
                    Int32.TryParse(mainDataKey.Substring(0, 3), out int currentBar);
                    
                    if (pattern.totalBarCount < currentBar)
                    {
                        pattern.totalBarCount = currentBar;
                    }

                    string channel = mainDataKey.Substring(3);
                    int lane = (channel[1] - '0') - 1;
                    // 2P (DP)인 경우 9를 더해서 2P 라인까지 index가 갈 수 있도록 수정
                    // Pattern.cs 참고
                    if (channel[0] == '2' || channel[0] == '6')
                    {
                        lane += 9;
                    }

                    // 롱노트 시작 / 종료 여부
                    bool isLntypeStarted = false;
                    // int prevLnBar = 0;
                    // int prevLnBeat = 0;

                    // 채널 02를 제외하고 모두 36진수로 이루어져 있어 채널 여부로 구분
                    if (channel != "02")
                    {
                        int beatLength = mainDataValue.Length / 2;

                        for (int i = 0; i < mainDataValue.Length - 1; i += 2)
                        {
                            int beat = i / 2;
                            // Value - 36진수에서 10진수로 파싱된 값
                            int parsedToIntValue = Decode36(mainDataValue.Substring(i, 2));
                            // Debug.Log($"{bar} 마디의 {beat}비트");

                            // 키음이 00이 아닐때만 노트, BGA 등 에셋 배치
                            if (parsedToIntValue == 0)
                            {
                                continue;
                            }

                            // 노트 처리 //
                            if (channel[0] == '1' || channel[0] == '2')
                            {
                                // 롱노트 - LNOBJ 선언 됐을 경우의 처리
                                if (TrackInfo.lnobj == parsedToIntValue)
                                {
                                    pattern.AddNote(lane, currentBar, beat, beatLength, parsedToIntValue, Note.NoteFlagState.LnEnd);
                                    continue;
                                }

                                // 일반 노트
                                pattern.AddNote(lane, currentBar, beat, beatLength, parsedToIntValue, Note.NoteFlagState.Default);
                                continue;
                            }
                            
                            // 롱노트 - LNTYPE 1 명령어 일 경우의 처리
                            if (TrackInfo.lnType == 1 && (channel[0] == '5' || channel[0] == '6'))
                            {
                                if (isLntypeStarted == true)
                                {
                                    pattern.AddNote(lane, currentBar, beat, beatLength, parsedToIntValue, Note.NoteFlagState.LnEnd);
                                    isLntypeStarted = false;
                                    continue;
                                }

                                isLntypeStarted = !isLntypeStarted;
                                continue;
                            }

                            // BGM CHANNEL //
                            if (channel == "01")
                            {
                                pattern.AddBGMKeySound(currentBar, beat, beatLength, parsedToIntValue);
                                continue;
                            }

                            // BPM CHANNEL //
                            if (channel == "03")
                            {
                                pattern.AddBPMTable(currentBar, beat, beatLength, parsedToIntValue);
                                continue;
                            }

                            // BGA SEQUENCE //
                            if (channel == "04")
                            {

                                continue;
                            }

                            // BPM CHANNEL 이전 BPM추가 //
                            if (channel == "08")
                            {
                                pattern.AddBPMTable(currentBar, beat, beatLength, TrackInfo.bpmTable[parsedToIntValue]);
                                continue;
                            }

                            // STOP GIMMIK Table //
                            if (channel == "09")
                            {
                                pattern.AddStop(currentBar, beat, beatLength, TrackInfo.stopTable[parsedToIntValue]);
                                continue;
                            }
                        }
                    }
                    else
                    {
                        // 변박 //
                        // 마디 내 박자 수 (1이 4/4 박자임.)
                        Double.TryParse(mainDataValue, out double beatMeasureLength);
                        pattern.AddBeatMeasureLength(currentBar, beatMeasureLength);
                    }
                }
            }
        }
    }
}