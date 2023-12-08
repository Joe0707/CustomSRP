using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestValue : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        int a = 1;
        int b = a;
        TestRefValue(ref a);
        print(a);
        print(b);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    void TestRefValue(ref int v)
    {
        v = 3;
    }
}
