using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyboardInteraction : MonoBehaviour
{

    private const string TABLE_NAME = "table";

    private const float POSITION_SHIFT_RATE = 0.001f;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKey(KeyCode.UpArrow))
        {
            Vector3 tablePosition = GameObject.Find(TABLE_NAME).transform.position;
            tablePosition.y += POSITION_SHIFT_RATE;
            GameObject.Find(TABLE_NAME).transform.position = tablePosition;
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            Vector3 tablePosition = GameObject.Find(TABLE_NAME).transform.position;
            tablePosition.y -= POSITION_SHIFT_RATE;
            GameObject.Find(TABLE_NAME).transform.position = tablePosition;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            // Check that fove is set up correctly
            if ((FoveInterface.IsHardwareConnected() && FoveInterface.IsHardwareReady()) == false)
                return;

            FoveInterface.TareOrientation();
            FoveInterface.TarePosition();
        }
    }
}
