using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class URGVisualizer : MonoBehaviour
{
    [SerializeField]
    private GameObject indicator;

    private List<GameObject> indicators = new List<GameObject>();
    private URGCommunication urg;

    // Start is called before the first frame update
    void Start()
    {
        urg = GetComponent<URGCommunication>();
        for (int i = 0; i < 1081; i++)
        {
            indicators.Add(
                Instantiate(indicator, new Vector3(indicator.transform.localScale.x * i, 0, 0), Quaternion.identity)
            );
        }
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(indicators.Count + " / " + urg.Distances.Count);
        for (int i = 0; i < indicators.Count; i++)
        {
            var modifiedScale = indicators[i].transform.localScale;
            modifiedScale.z = urg.Distances[i] / 10;

            indicators[i].transform.localScale = modifiedScale;
        }

    }
}
