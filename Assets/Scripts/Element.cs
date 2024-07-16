using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class Element : MonoBehaviour
{
    public String symbol = "H";
    public int atomicNumber = 1;
    public TextMeshPro symbolText;
    public TextMeshPro atomicNumberText;
    public int index;
    public float angleOnOrbit;
    public float radiusOfOrbit;
    public bool isAligning;
    public bool isPrime;
    public ForceField forceField;
    
    public SpriteRenderer spriteRenderer;

    public CircleCollider2D _collider2D;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        _collider2D = GetComponent<CircleCollider2D>();

        this.radiusOfOrbit = GetComponentInParent<ForceField>().radius == 0 ? 4f: GetComponentInParent<ForceField>().radius ;
    }

    public void GetIntoOrbit(ForceField f, float angle)
    {
        this.angleOnOrbit = GetNormalizedAngle(angle);
        this.radiusOfOrbit = f.radius ;

        transform.position = GetPointAwayAtAngle(f.transform.position, this.radiusOfOrbit, angle);

        this.forceField = f;
    }

    public IEnumerator MoveToOrbit(ForceField f, float angle)
    {
        float current = 0;

        this.angleOnOrbit = angle;

        List<Element> sorted = f.elements.OrderBy(e => e.angleOnOrbit).ToList();

        f.elements = sorted;
        f.shootIndex = sorted.IndexOf(this);
        float target = f.radius ;

        bool isNewOne = false;
        Element nextElement = null;

        float t = 0;
        float t1 = 0;
        while (current < target)
        {
            transform.position = GetPointAwayAtAngle(f.transform.position, current, angle);
            
            current += target * Time.deltaTime/f.shootRate;

            if (current >= f.radius * f.radiusFactorOfElement*2 && !isNewOne)
            {
                nextElement = Instantiate(f.elementPrefab, f.transform);
                nextElement.transform.localScale = Vector3.zero;

                t1 = target-current;
                nextElement.SetState(f.allElementStates[f.GetRandomState()]);

                isNewOne = true;
            }

            if (t < t1)
            {
                t += target*Time.deltaTime/f.shootRate;
                nextElement.transform.localScale = Vector3.one * f.radius * f.radiusFactorOfElement * t / t1;
                if (t >= t1) nextElement.transform.localScale = Vector3.one * f.radius * f.radiusFactorOfElement;
            }

            yield return new WaitForEndOfFrame();
        }

        this.forceField = f;

        nextElement.enabled = true;
        var colorOfField = nextElement.spriteRenderer.color;

        float h, s, v;
        Color.RGBToHSV(colorOfField, out h, out s, out v);
        v = 1f;

        Color newColor = Color.HSVToRGB(h, s, v);
        newColor.a = f.spriteRenderer.color.a;
        f.spriteRenderer.color = newColor;
        f.isShooting = false;
        f.target = null;


        if (nextElement.atomicNumber >= f.highestAtomicNo) f.highestAtomicNo = nextElement.atomicNumber;
        f.UpdateHighestAtom();
        sorted.Add(nextElement);

        int i = 0;

        foreach(Element e in sorted)
        {
            e.index = i;
            i++;
        }



        f.elements = sorted;
        f.CheckForCollision();

        yield return null;
    }

    public static Vector3 GetPointAwayAtAngle(Vector3 center, float radius, float angle)
    {
        Vector3 output = center;
        output.x += radius * Mathf.Cos(angle * Mathf.Deg2Rad);
        output.y += radius * Mathf.Sin(angle * Mathf.Deg2Rad);
        return output;
    }
    void Update()
    {

        angleOnOrbit = GetNormalizedAngle(angleOnOrbit);

        if (forceField != null && this != forceField.target && !forceField.isColliding && radiusOfOrbit != 0) {
            index = forceField.elements.IndexOf(this);
            if (!forceField.isShooting)
            {
                angleOnOrbit += forceField.orbitalAngularSpeed * Time.deltaTime;
                isAligning = false;
            }
            else
            {
                
                if (!isAligning)
                {



                    float targetAngle = forceField.shootAngle;
                    float di = index - forceField.shootIndex;

                    targetAngle += (360 * di) / (forceField.elements.Count);
                    StartCoroutine(AlignTo(targetAngle,forceField.shootRate));
                    
                    isAligning = true;
                }


            }


        }

        angleOnOrbit = GetNormalizedAngle(angleOnOrbit);
        if(forceField != null) transform.position = GetPointAwayAtAngle(new Vector3(forceField.transform.position.x, forceField.transform.position.y, transform.position.z), this.radiusOfOrbit, angleOnOrbit);


    }
    public IEnumerator AlignTo(float target,float rate)
    {
        float d = target - angleOnOrbit;
        float start = angleOnOrbit;

        if (d > 180)
        {
            d = d - 360; 
        }

        if (d < -180)
        {
            d = d + 360;
        }
        float t = 0;
        

        while (t < rate)
        {
            t += Time.deltaTime;
            angleOnOrbit  = start + d * t/rate;

            yield return new WaitForEndOfFrame();
        }
        yield return null;
    }
    public static float GetNormalizedAngle(float angle){
        while(angle < 0) angle += 360;
        while(angle > 360) angle -=360;
        return angle;
    }
    public static float GetAngleBetween(Vector3 centre, Vector3 target)
    {

        var d = target - centre;

        if (d.x < 0 && d.y == 0)
        {
            return 180;
        }
        if (d.x > 0 && d.y == 0)
        {
            return 0;
        }

        if (d.x == 0 && d.y > 0)
        {
            return 90;
        }

        if (d.x == 0 && d.y < 0)
        {
            return -90;
        }

        var a = Mathf.Atan(d.y / d.x) * Mathf.Rad2Deg;
        if (d.x < 0 && d.y > 0)
        {
            a += 180;
        }
        else if (d.x < 0 && d.y < 0)
        {
            a -= 180;
        }


        return GetNormalizedAngle(a);
    }
    float GetAngleTo(Vector3 target)
    {
        return GetAngleBetween(transform.position, target);
    }

    public void SetState(ElementState state)
    {
        spriteRenderer.color = state.color;
        atomicNumber = state.atomicNumber;
        symbol = state.symbol;

        symbolText.color = state.colorSecondary;
        symbolText.text = state.symbol;

        atomicNumberText.text = atomicNumber + "";
        if (atomicNumber == 0) atomicNumberText.text = "";

    }


}
