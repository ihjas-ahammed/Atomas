using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UI;

public class ForceField : MonoBehaviour
{

    public List<Element> elements;
    public float radius = 4f;
    public Element elementPrefab;
    public List<ElementState> allElementStates;
    public SpriteRenderer spriteRenderer;
    public List<GameObject> endDots;
    public ParticleSystem colliderPrefab;
    public float shootRate = 0.25f;

    public float bgScale0;
    public Vector3 scoreAndLabelPosition0;

    public Button newGameBtn;
    public Vector2 scoreAndLabelSizeDelta0;

    public RectTransform scoreAndLabel;


    public Transform backgroundImage;

    public float gameOverSpeed = 0.1f;
    public float collisionSpeed = 0.15f;

    public TextMeshProUGUI highestAtomName;
    public int highestAtomicNo = 1;

    int plusRadar = 0;

    public int score = 0;
    public TextMeshProUGUI scoreText;

    public float orbitalAngularSpeed = 10f;
    public int maxElements = 14;
    public bool isShooting;
    public bool isColliding;

    public float shootAngle;
    public int shootIndex;
    public float collisionAngle;
    public int CollisionIndex;
    public bool isGameOver;
    public float radiusFactorOfElement = 0.03f;

    public Element target;

    void Start()
    {

        transform.localScale = Vector3.one * (radius + 1) * 2;



        foreach (Element e in GetComponentsInChildren<Element>())
        {
            elements.Add(e);
            e.transform.localScale = Vector3.one * radius * radiusFactorOfElement;
        };

        elementPrefab.transform.localScale = Vector3.one* radius *0.03f;

        bgScale0 = backgroundImage.transform.localScale.x;
        scoreAndLabelPosition0 = scoreAndLabel.position;
        scoreAndLabelSizeDelta0 = scoreAndLabel.sizeDelta;

        NewGame();
    }

    public void NewGame()
    {
        scoreAndLabel.position = scoreAndLabelPosition0;
        backgroundImage.transform.localScale = Vector3.one * bgScale0;
        scoreAndLabel.sizeDelta = scoreAndLabelSizeDelta0;
        newGameBtn.gameObject.SetActive(false);
        newGameBtn.GetComponent<CanvasGroup>().alpha = 0f;

        isGameOver = false;
        Destroy(elements[0].gameObject);
        elements.Clear();
        elements.Add(Instantiate(elementPrefab, transform));

        for (int i = 0; i < 4; i++)
        {
            Element e = Instantiate(elementPrefab, transform);

            int state = GetRandomState();

            while (state == allElementStates.Count - 1)
            {
                state = GetRandomState();
            }

            e.SetState(allElementStates[state]);
            if(e.atomicNumber >= highestAtomicNo) highestAtomicNo = e.atomicNumber;
            elements.Add(e);
        }


        for (int i = 0; i < (elements.Count - 1); i++)
        {
            elements[i].GetIntoOrbit(this, (i * 360) / (elements.Count - 1));
        }

        foreach(Element e in elements)
        {
            e.transform.localScale = Vector3.one * radius * radiusFactorOfElement;
        }

        score = 0;
        isShooting = false;

        UpdateHighestAtom();
    }

    public void UpdateHighestAtom()
    {

        ElementState es = allElementStates[highestAtomicNo - 1];

        float h, s, v;
        Color.RGBToHSV(es.color, out h, out s, out v);
        v = 1f;

        Color newColor = Color.HSVToRGB(h, s, v);
        highestAtomName.color = newColor;
        highestAtomName.text = es.label;
    }
    void Update()
    {
        scoreText.text = score + "";

        if (Input.GetMouseButtonDown(0) && !isShooting && !isColliding && !isGameOver & !IsAligning())
        {
            Shoot();
        }
        if (elements.Count > maxElements && !isGameOver)
        {
            isGameOver = true;
            StopAllCoroutines();
            StartCoroutine(GameOver());
        }

        if (elements.Count > maxElements - 4 && !isGameOver)
        {

            foreach (GameObject g in endDots)
            {
                g.SetActive(true);
            }
            int j = 2;
            for (int i = maxElements - 3; i < maxElements; i++)
            {
                if (elements.Count > i)
                {
                    endDots[j].SetActive(false);
                    j -= 1;
                }
            }
        }
        else
        {
            foreach (GameObject g in endDots)
            {
                g.SetActive(false);
            }
        }


    }

    bool IsAligning()
    {
        bool isAligning = false;

        foreach(Element e in elements)
        {
            if (e.isAligning ) return true;
        }

        return isAligning;
    }

    IEnumerator GameOver()
    {
        Element element1 = elements.OrderBy(e => e.atomicNumber).ToList()[elements.Count - 1];
        element1.transform.position = element1.transform.position - 0.5f * Vector3.forward;

        float t = 0;

        float s0 = backgroundImage.localScale.x;
        float s1 = radiusFactorOfElement;


        ParticleSystem collider = Instantiate(colliderPrefab, element1.transform);
        collider.startColor = element1.spriteRenderer.color;
        while (t < gameOverSpeed)
        {
            t += Time.deltaTime;

            element1.radiusOfOrbit -= (radius / gameOverSpeed) * Time.deltaTime;
            if(element1.radiusOfOrbit < 0) element1.radiusOfOrbit = 0;
            
           
            yield return new WaitForEndOfFrame();
        }


        elements.Remove(element1);
        Element[] k = elements.ToArray<Element>();

        for (int i = elements.Count - 1; i >= 0; i--)
        {
            Element element = k[i];
            var tA0 = element.angleOnOrbit;
            bool isCollided = false;

            t = 0;
            while (t < gameOverSpeed && !isCollided)
            {
                t += Time.deltaTime;

                element.radiusOfOrbit -= (radius / gameOverSpeed)* Time.deltaTime;
                if(element.radiusOfOrbit < 0) element.radiusOfOrbit=0;

                if (Vector2.Distance((Vector2)element1.transform.position, (Vector2)element.transform.position) < radiusFactorOfElement * radius*2 )
                {
                    StartCoroutine(Ripple(element1, gameOverSpeed / 2));
                    isCollided = true;
                }

                yield return new WaitForEndOfFrame();
            }



            collider.startColor = element.spriteRenderer.color;
            collider.Play();


            elements.Remove(element);
            if (element != null ) Destroy(element.gameObject);
            
            foreach(Element e in elements)
            {
                var targetAngle = tA0;
                float di = elements.IndexOf(e);

                targetAngle += (360 * di) / (elements.Count);
                StartCoroutine(e.AlignTo(targetAngle,gameOverSpeed));
            }
        }


        elements.Clear();
        elements.Add(element1);
        element1.transform.position = transform.position;

        StartCoroutine(StartGameOverAnimation());

        yield return null;
    }

    IEnumerator StartGameOverAnimation()
    {
        float t = 0;
        float t0 = gameOverSpeed * 2;

        float s = backgroundImage.localScale.x;
        float s1 = s;
        float s0 = radiusFactorOfElement;

        float y = scoreAndLabel.position.y;
        float y0 = y - 300; 
        float y1 = y;


        float w= scoreAndLabel.sizeDelta.x;
        
        
        float w0 = Screen.width;
        float dw = w0 - w;
        
        float r = scoreAndLabel.sizeDelta.y/scoreAndLabel.sizeDelta.x;

        newGameBtn.gameObject.active = true;

        while (t < t0)
        {
            if(s > s0)
            {
                s -= ((s1 - s0) / t0) * Time.deltaTime;
                backgroundImage.localScale = Vector3.one * s;
            }

            if(y > y0)
            {
                y -= ((y1 - y0) / t0) * Time.deltaTime;
                Vector2 pos = scoreAndLabel.position;
                pos.y = y;
                scoreAndLabel.position = pos;
            }

            if(w < w0)
            {
                w += dw *Time.deltaTime/ t0;
                Vector2 sizeDelta = scoreAndLabel.sizeDelta;
                sizeDelta.x = w;
                sizeDelta.y = w * r;
                scoreAndLabel.sizeDelta = sizeDelta;
            }

            t += Time.deltaTime;

            newGameBtn.GetComponent<CanvasGroup>().alpha = t / t0;

            yield return new WaitForEndOfFrame();
        }
        yield return null;
    }
    void Shoot()
    {
        if (isGameOver)
        {
            return;
        }

        var mouseInput = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseInput.z = transform.position.z;
        if (Mathf.Abs(Vector3.Distance(transform.position, mouseInput)) > radius + 1) return;
        
        float targetAngle = Element.GetAngleBetween(transform.position,mouseInput);
        
        

        this.isShooting = true;
        this.shootAngle = targetAngle;

        var lastElement = elements[elements.Count - 1];


        

        StartCoroutine(lastElement.MoveToOrbit(this, targetAngle));


        
    }

    public void CheckForCollision()
    {
        for (int i = 0; i < elements.Count - 1;i++) {
            var element = elements[i];
            if(element.atomicNumber == 0 )
            {
                Element oldTarget = element;
                int index =elements.IndexOf(element);


                Element after = elements[NormalizeInRange(index + 1, 0, elements.Count - 1)];
                Element before = elements[NormalizeInRange(index - 1, 0, elements.Count - 1)];
                if (after.atomicNumber == before.atomicNumber && oldTarget.atomicNumber == 0)
                {
                    oldTarget.transform.position = new Vector3(oldTarget.transform.position.x, oldTarget.transform.position.y, -0.5f);
                    this.isColliding = true;
                    StartCoroutine(Collide(after, before, oldTarget.angleOnOrbit,oldTarget,false));
                }
            }
        }

        
    }

    IEnumerator Collide(Element a,Element b,float centerAngle,Element oldTarget,bool isChainReaction)
    {
        float da = GetShortestAngleDisplacement(centerAngle - a.angleOnOrbit);
        float db = GetShortestAngleDisplacement(centerAngle - b.angleOnOrbit);

        float startA = a.angleOnOrbit;
        float startB = b.angleOnOrbit;

        float t = 0;
        bool isCollided = false;

        while (t < collisionSpeed &&  !isCollided)
        {
            t += Time.deltaTime;
            a.angleOnOrbit = startA + da * t / collisionSpeed;
            b.angleOnOrbit = startB + db * t / collisionSpeed;

            a.transform.position = Element.GetPointAwayAtAngle(transform.position, radius, a.angleOnOrbit);
            b.transform.position = Element.GetPointAwayAtAngle(transform.position, radius, b.angleOnOrbit);

            if (Vector3.Distance(a.transform.position,b.transform.position) < radiusFactorOfElement*radius  ) {
                StartCoroutine(Ripple(oldTarget,collisionSpeed /2));
                isCollided = true;
            }

            yield return new WaitForEndOfFrame();
        }


        Destroy(a.gameObject);
        Destroy(b.gameObject);


        elements.Remove(a);
        elements.Remove(b);


        ElementState nextState = allElementStates[ oldTarget.atomicNumber > a.atomicNumber ? oldTarget.atomicNumber : a.atomicNumber];
        oldTarget.SetState(nextState);

        if (oldTarget.atomicNumber >= highestAtomicNo) highestAtomicNo = oldTarget.atomicNumber;
        UpdateHighestAtom();

        score += UnityEngine.Random.Range(2, 5) * oldTarget.atomicNumber;

        for (int i = 0; i < elements.Count - 1; i++)
        {
            Element e = elements[i];
            if (e == oldTarget) continue;

            float targetAngle = oldTarget.angleOnOrbit;
            float di = i - elements.IndexOf(oldTarget);

            targetAngle += (360 * di) / (elements.Count-1);
            StartCoroutine(e.AlignTo(targetAngle,collisionSpeed));
        }

        if (elements.Count-1 >= 3) {
            int index = elements.IndexOf(oldTarget);
            Element after = elements[NormalizeInRange(index + 1, 0, elements.Count - 1)];
            Element before = elements[NormalizeInRange(index - 1, 0, elements.Count - 1)];

            if(after.atomicNumber == before.atomicNumber)
            {

                this.isColliding = true;
                ParticleSystem collider = Instantiate(colliderPrefab, oldTarget.transform);
                collider.startColor = oldTarget.spriteRenderer.color;
                collider.Play();
                StartCoroutine(Collide(after, before, oldTarget.angleOnOrbit, oldTarget,true));
            }
            else
            {

                oldTarget.transform.position = new Vector3(oldTarget.transform.position.x, oldTarget.transform.position.y, 0);
                ParticleSystem collider = Instantiate(colliderPrefab,oldTarget.transform);
                collider.startColor = oldTarget.spriteRenderer.color;
                collider.Play();
                isColliding = false;
            }

        }
        else
        {

            oldTarget.transform.position = new Vector3(oldTarget.transform.position.x, oldTarget.transform.position.y, 0);
            ParticleSystem collider = Instantiate(colliderPrefab, oldTarget.transform);
            collider.startColor = oldTarget.spriteRenderer.color;
            collider.Play();
            isColliding = false;
        }



        yield return null;
    }
    
    IEnumerator Ripple(Element target,float time)
    {
        var minScale = target.transform.localScale.x;
        float t = 0;
        while(t < time)
        {
            t += Time.deltaTime;
            target.transform.localScale = Vector3.one * minScale * (1f + 0.1f*t/time);
            yield return new WaitForEndOfFrame();
        }

        while (t > 0)
        {
            t -= Time.deltaTime;
            target.transform.localScale = Vector3.one * minScale * (1f + 0.1f * t / time);
            yield return new WaitForEndOfFrame();
        }

        target.transform.localScale = Vector3.one * minScale;

        yield return null;
    }

    public int GetRandomState()
    {
        int limit = elements.OrderBy(e => e.atomicNumber > 0 ? e.atomicNumber : 1).ToList()[elements.Count-1].atomicNumber-1;
        int start = elements.OrderBy(e => e.atomicNumber > 0 ? e.atomicNumber : 1).ToList()[0].atomicNumber-2;
        if (start < 0) start = 0;
        

        if (limit < 1) limit = 3;

        int m =  UnityEngine.Random.Range(start,limit);

        if(elements.FindAll(e => e.atomicNumber == (start == 0 ? 1 : start + 2) ).ToList().Count > 4){
            if (UnityEngine.Random.Range(0, 2) == 1)
                return allElementStates.Count - 1;
        }

        if (UnityEngine.Random.Range(0, maxElements-elements.Count) == 1)
        {
            return allElementStates.Count - 1;
        }

        if(elements.Count <= maxElements - 3)
        {
            if (UnityEngine.Random.Range(0, 1) == 1)
                return allElementStates.Count - 1;
        }
        if (m != allElementStates.Count - 1) {
            plusRadar++;
            if (plusRadar >= 5)
            {
                plusRadar = 0;
                m = allElementStates.Count - 1;
            }
        }

        return m;
    }


    float GetShortestAngleDisplacement(float angle)
    {
        float d = angle;
        while(d > 180)
        {
            d = d - 360;
        }

        while(d < -180)
        {
            d = d + 360;
        }
        return d;
    }


    int NormalizeInRange(int value,int min,int max)
    {
        int output = value;
        int delta = max - min;
        while(output >= max) output-=delta;
        while(output < min) output+=delta;

        return output;
    }
}


