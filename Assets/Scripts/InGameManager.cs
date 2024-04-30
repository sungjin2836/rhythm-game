using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InGameManager : MonoBehaviour
{
    BMSParser bmsParser = null;
    TrackInfo patternData = null;

    void Start()
    {
        // 데이터 파싱 처리
        bmsParser = new BMSParser(GameManager.Instance.selectedTrack);
        bmsParser.ParseMainData();
        patternData = bmsParser.TrackInfo;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
