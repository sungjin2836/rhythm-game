using System;
using System.IO;

public class BMSParser
{
    TrackInfo trackInfo = new TrackInfo();
    public TrackInfo TrackInfo { get { return trackInfo; } }
    bool isRandom = false;
    bool isIfStatementTrue = false;
    bool isCheckIfstatementStarted = false;
    int randomResult = 0;
    string path = "";

    public BMSParser(string path)
    {
        this.path = path;
    }

    public void ParseHeader(string line)
    {
        string headerKey = line.IndexOf(" ") > -1 && line.StartsWith("#") ? line.Substring(0, line.IndexOf(" ")) : line;
        string headerValue = line.IndexOf(" ") > -1 && line.StartsWith("#") ? line.Substring(line.IndexOf(" ") + 1) : "";

        // Random에 대한 전처리 (#RANDOM 숫자 / #ENDRANDOM으로 구분) //
        // 랜덤 값 지정
        if (headerKey == "#RANDOM")
        {
            isRandom = true;
            Int32.TryParse(headerValue, out int randomNumber);
            randomResult = new System.Random().Next(1, randomNumber + 1);
        }

        // 랜덤 탈출
        if (headerKey == "#ENDRANDOM")
        {
            isRandom = false;
            randomResult = 0;
        }

        // 조건에 대한 전처리 (#IF 숫자 / #ENDIF로 구분) // 
        // 조건 검색
        if (headerKey == "#IF" && Int32.TryParse(headerValue, out int parsedHeaderValueNumber) && isRandom == true)
        {
            isCheckIfstatementStarted = true;
            if (parsedHeaderValueNumber == randomResult)
            {
                isIfStatementTrue = true;
            }
            else
            {
                isIfStatementTrue = false;
            }
        }

        // 조건 탈출
        if (headerKey == "#ENDIF")
        {
            isCheckIfstatementStarted = false;
            isIfStatementTrue = false;
        }

        // 트랙 정보 지정
        if (isRandom == false || (isIfStatementTrue == true && isCheckIfstatementStarted == true) || isCheckIfstatementStarted == false)
        {
            switch (headerKey)
            {
                case "#PLAYER":
                    Int32.TryParse(headerValue, out trackInfo.playerType);
                    break;
                case "#GENRE":
                    trackInfo.genre = headerValue;
                    break;
                case "#TITLE":
                    trackInfo.title = headerValue;
                    break;
                case "#ARTIST":
                    trackInfo.artist = headerValue;
                    break;
                case "#PLAYLEVEL":
                    Int32.TryParse(headerValue, out trackInfo.playLevel);
                    break;
                case "#RANK":
                    Int32.TryParse(headerValue, out trackInfo.rank);
                    break;
                case "#TOTAL":
                    Single.TryParse(headerValue, out trackInfo.total);
                    break;
                case "#STAGEFILE":
                    trackInfo.stageFile = Path.Combine(Directory.GetParent(path).FullName, headerValue);
                    break;
            }

            // 파일 정보 관련 파싱 //
            // 오디오
            if (headerKey.StartsWith("#WAV"))
            {
                trackInfo.audioFileNames.Add(headerKey.Substring(4), Path.Combine(Directory.GetParent(path).FullName, System.Web.HttpUtility.UrlEncode(Path.GetFileNameWithoutExtension(headerValue))));
            }
            // BGA 이미지 (mp4도 있을수도 있음.)
            if (headerKey.StartsWith("BMP"))
            {
                trackInfo.imageFileNames.Add(headerKey.Substring(4), Path.Combine(Directory.GetParent(path).FullName, System.Web.HttpUtility.UrlEncode(Path.GetFileNameWithoutExtension(headerValue))));
            }

            // BPM //
            if (line.StartsWith("#BPM"))
            {
                if (line[4] == ' ')
                {
                    // 일반 BPM
                    float.TryParse(headerValue, out trackInfo.bpm);
                }
                else
                {
                    // 가변 BPM
                    string[] bpmKeyValuePair = line.Substring(4).Split(" ");
                    string bpmKey = bpmKeyValuePair[0];
                    Single.TryParse(bpmKeyValuePair[1], out float bpmValue);

                    trackInfo.bpmTable.Add(bpmKey, bpmValue);
                }
            }
        }
    }

    public void ResetRandomState()
    {
        isRandom = false;
        isIfStatementTrue = false;
        isCheckIfstatementStarted = false;
        randomResult = 0;
        path = "";
    }

    public void ParseMainData(string line)
    {

    }
}