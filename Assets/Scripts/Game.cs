using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour
{
    public static event System.Action OnGameOver;
    public static Game Instance {get; private set;}

    public Sheep sheepPrefab;
    public int sheepCount;
    public float maxRadius;
    public float scale;
    public float minOffset;

    [Space(10.0f)]
    public float world_size = 16.0f;
    public LayerMask terrainLayer;
    public float gameTime = 60.0f;

    public TMPro.TextMeshProUGUI counterText;
    public Image timer;
    public Gradient timerGradient;
    public AudioSource sheepDieSource;

    [Header("gameover")]
    public const string gameoverTextFormat = "Game over! Thanks for trying it out.\nSheeps caught: <b>{0}</b>";
    public TextMeshProUGUI gameoverText;
    public RectTransform gameoverTransform;
    
    public AnimationCurve fadeCurve;
    public float fadeSpeed;
    public RectTransform startScreen;

    public Image exitbar;
    public float timeRequiredToExit = 0.5f;
    public float currentTimeToExit = 0.0f;

    public RectTransform replayButtons;

    List<Sheep> sheeps;
    Transform sheepContainer;
    int killedSheepCount;
    bool gameIsOver;
    bool hasCheated;

    public float Elapsed => timeElapsed;
    public float Elapsed_T => Mathf.Clamp01(timeElapsed / gameTime);
    float timeElapsed;

    private void Awake() {
        Instance = this;
        gameIsOver = false;
        hasCheated = false;
        timeElapsed = 0.0f;
        timer.fillAmount = 1.0f;
        killedSheepCount = 0;
        currentTimeToExit = 0.0f;
        counterText.text = killedSheepCount.ToString();
    
        gameoverTransform.gameObject.SetActive(false);
        exitbar.transform.parent.gameObject.SetActive(true);
        startScreen.transform.gameObject.SetActive(true);
        replayButtons.gameObject.SetActive(false);
        
        Physics.autoSimulation = false;
    }

    [ContextMenu("reset times played")]
    void ResetPlayerPrefs ()
    {
        Debug.Log("previous value: " + PlayerPrefs.GetInt("times_played", 0));
        PlayerPrefs.SetInt("times_played", 0);
    }

    private void Start() {
        Time.timeScale = 0.0f;
    }

    public void StartButton_Start ()
    {
        StartCoroutine(FadeScreen(startScreen, true, ()=>{
            Time.timeScale = 1.0f;
            startScreen.transform.gameObject.SetActive(false);
            sheepContainer = new GameObject("sheep_container").transform;

            int p = PlayerPrefs.GetInt("times_played", 0);
            replayButtons.gameObject.SetActive((p > 0));
            
            PlayerPrefs.SetInt("times_played", p+1);

            Physics.autoSimulation = true;
            SpawnSheeps ();
        }));
    }

    public void SpawnSheepButton ()
    {
        if(!gameIsOver)
        {
            hasCheated = true;
            SpawnSheeps();
        }
    }

    public void SpawnSheeps ()
    {
        if(sheeps == null)
            sheeps = new List<Sheep>(sheepCount);

        Vector2[] ps = GenerateSunflowerPoints();
        Vector3 worldcenter = new Vector3(world_size * 0.5f, 0.0f, world_size * 0.5f);
            
        for (int i = 0; i < ps.Length; i++) {

            Vector3 p = new Vector3(ps[i].x, 7.0f, ps[i].y) + worldcenter;            
            if(Physics.Raycast(p, Vector3.down, out RaycastHit hit, 100.0f, terrainLayer.value))
                p.y = hit.point.y + 0.4f;
            
            Sheep sh = Instantiate(sheepPrefab);
            sh.transform.SetParent(sheepContainer);
            sh.Spawn(p);
            sheeps.Add(sh);
        }
    }

    public void SpawnHole ()
    {
        if(!gameIsOver)
        {
            Hole hole = FindObjectOfType<Hole>();
            if(hole != null)
            {
                hasCheated = true;
                Instantiate(hole);
            }
        }
    }

    private void Update() {
        if(!gameIsOver)
        {
            if(currentTimeToExit <= 0.0f)
                exitbar.transform.parent.gameObject.SetActive(false);

            if(Input.GetKey(KeyCode.Escape))
            {
                exitbar.transform.parent.gameObject.SetActive(true);
                currentTimeToExit += Time.deltaTime;
                exitbar.fillAmount = (currentTimeToExit / timeRequiredToExit);
            }
            else
            {
                currentTimeToExit -= Time.deltaTime * 2.0f;
                exitbar.fillAmount = (currentTimeToExit / timeRequiredToExit);
            }

            if(currentTimeToExit >= timeRequiredToExit)
            {
                Application.Quit();
#if UNITY_EDITOR
                Debug.Break();
#endif
                return;
            }
            currentTimeToExit = Mathf.Max(currentTimeToExit, 0.0f);

            float t = 1.0f - Elapsed_T;
            timeElapsed += Time.deltaTime;
            timer.fillAmount = t;
            timer.color = timerGradient.Evaluate(t);

            if(timeElapsed > gameTime)
                GameOver();
        }
    }

    public void KillSheep (Sheep sheep) {
        sheepDieSource.Play();
        sheeps.Remove(sheep);
        killedSheepCount++;
        counterText.text = killedSheepCount.ToString();

        if(sheeps.Count == 0)
            SpawnSheeps();

        Destroy(sheep, 3.0f);
    }

    void GameOver ()
    {
        Physics.autoSimulation = false;
        OnGameOver?.Invoke();
        gameIsOver = true;
        timer.transform.parent.gameObject.SetActive(false);
        counterText.gameObject.SetActive(false);
    
        // SHOW GAMEOVER
        gameoverText.text = string.Format(gameoverTextFormat, killedSheepCount);
        if(hasCheated)
            gameoverText.text += " (cheated)";

        StartCoroutine(FadeScreen(gameoverTransform, false, ()=>{}));
    }


    IEnumerator FadeScreen (RectTransform transform, bool invert, System.Action callback)
    {
        transform.gameObject.SetActive(true);
    
        Image[] images = transform.GetComponentsInChildren<Image>();

        float t = 0.0f;
        while (t <= 1.0f)
        {
            t += Time.unscaledDeltaTime / fadeSpeed;
            
            float tt = fadeCurve.Evaluate(t);
            if(invert)
                tt = 1.0f - tt;
    
            for (int i = 0; i < images.Length; i++)
            {
                Color c = images[i].color;
                c.a = Mathf.Clamp01(tt);
                images[i].color = c;
            }

            yield return null;
        }


        for (int i = 0; i < images.Length; i++)
        {
            Color c = images[i].color;
            c.a = invert ? 0.0f : 1.0f;
            images[i].color = c;
        }

        callback?.Invoke();
    }

    public void GameOverButton_Exit ()
    {
        Application.Quit();
    }

    public void GameOverButton_Restart ()
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnDrawGizmos() {
        if(Application.isPlaying)
        {
            Vector2[] ps = GenerateSunflowerPoints();
            Vector3 worldcenter = new Vector3(world_size * 0.5f, 0.0f, world_size * 0.5f);

            for (int i = 0; i < ps.Length; i++)
            {
                Vector3 p = new Vector3(ps[i].x, 0.0f, ps[i].y) + worldcenter;
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(p, 0.05f);
            }
        }
    }
    
    Vector2[] GenerateSunflowerPoints () {
        float it = Mathf.PI*(3-Mathf.Sqrt(5));
        Vector2[] positions = new Vector2[sheepCount];

        for(var i = 0; i< sheepCount; i++) 
        {
            // Calculating polar coordinates theta (t) and radius (r)
            float t = it * i; 
            float r = minOffset + Mathf.Sqrt(((float)i / sheepCount))  * maxRadius;

            // Converting to the Cartesian coordinates x, y
            float x = (scale * r * Mathf.Cos(t)); 
            float y = (scale * r * Mathf.Sin(t));

            positions[i] = new Vector2(x,y);
        }

        return positions;
    }
}
