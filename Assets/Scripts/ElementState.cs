using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName ="Element Data")]
public class ElementState : ScriptableObject
{
    public string label;
    public string symbol;
    public int atomicNumber;

    public Color color;
    public Color colorSecondary;
}
