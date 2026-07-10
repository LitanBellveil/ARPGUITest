using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class UIHorizontalPinch : MonoBehaviour
{
    [Range(0,1)]
    public float Amount;

    [Range(1,8)]
    public float Power = 3f;

    Material runtimeMat;

    static readonly int AmountID =
        Shader.PropertyToID("_Amount");

    static readonly int PowerID =
        Shader.PropertyToID("_Power");

    void Awake()
    {
        Graphic g = GetComponent<Graphic>();

        runtimeMat = Instantiate(g.material);
        g.material = runtimeMat;
    }

    void Update()
    {
        runtimeMat.SetFloat(AmountID, Amount);
        runtimeMat.SetFloat(PowerID, Power);
    }

    void OnDestroy()
    {
        if(runtimeMat)
            Destroy(runtimeMat);
    }
}