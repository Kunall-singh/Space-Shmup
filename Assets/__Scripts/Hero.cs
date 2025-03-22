using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Hero : MonoBehaviour
{
    static public Hero S { get; private set; }

    [Header("Inscribed")]

    public float speed =30;
    public float rollMult = -45;
    public float pitchMult = 30;
    public GameObject projectilePrefab;
    public float projectileSpeed = 40;
    public Weapon[] weapons;
    public bool isInvincible = false;
    private Material[] heroMaterials;
    private Color[] originalColors;
    public Color blinkColor = Color.white;

    [Header("Dynamic")][Range(0,4)][SerializeField]
    private float _shieldLevel = 4;
    [Tooltip ("This field holds a reference to the last triggering GameObject")]
    private GameObject lastTriggerGo = null;
    public delegate void WeaponFireDelegate();
    public event WeaponFireDelegate fireEvent;

    void Awake(){
        if(S == null){
            S = this;
        }
        else{
            Debug.LogError("Hero.Awake() - Attempted to assign second Hero.S!");
        }

        ClearWeapons();
        weapons[0].SetType(eWeaponType.blaster);
        Renderer[] rends = GetComponentsInChildren<Renderer>();
        heroMaterials = new Material[rends.Length];
        originalColors = new Color[rends.Length];

        for (int i = 0; i < rends.Length; i++) {
            heroMaterials[i] = rends[i].material;
            originalColors[i] = heroMaterials[i].color;
        }
    }

    void Update(){
        float hAxis = Input.GetAxis("Horizontal");
        float vAxis = Input.GetAxis("Vertical");

        Vector3 pos = transform.position;
        pos.x += hAxis * speed * Time.deltaTime;
        pos.y += vAxis * speed * Time.deltaTime;
        transform.position = pos;

        transform.rotation = Quaternion.Euler(vAxis*pitchMult, hAxis*rollMult, 0);

        if(Input.GetAxis("Jump") == 1 && fireEvent != null){

            fireEvent();
        }
    }

    void OnTriggerEnter(Collider other) {
        Transform rootT = other.gameObject.transform.root;
        GameObject go = rootT.gameObject;
        

        if(go == lastTriggerGo) return;
        lastTriggerGo = go;

        Enemy enemy = go.GetComponent<Enemy>();
        PowerUp pUp = go.GetComponent<PowerUp>();
        if(enemy != null){
            if(!isInvincible){
                shieldLevel--;
            }
            Destroy(go);
        }else if(pUp != null){
            AbsorbPowerUp(pUp);
        }
        else{
            Debug.LogWarning("Shield Trigger hit by non-Enemy: " +go.name);
        }
    }

    public void AbsorbPowerUp(PowerUp pUp ){
        Debug.Log("Absorbed PowerUp: " + pUp.type);
        switch(pUp.type){
            case eWeaponType.shield:
            shieldLevel++;
            break;

            default:
            if (pUp.type == eWeaponType.invincibility) {
                StartCoroutine(TempInvincibility());
                break;
            }

            if(pUp.type == weapons[0].type){
                Weapon weap = GetEmptyWeaponSlot();
                if(weap != null){
                    weap.SetType(pUp.type);
                }
            }else{
                ClearWeapons();
                weapons[0].SetType(pUp.type);
            }
            break;
        }
        pUp.AbsorbedBy(this.gameObject);
    }

    public float shieldLevel{
        get{return(_shieldLevel);}
        private set{
            _shieldLevel = Mathf.Min(value, 4);
            if(value < 0){
                Destroy(this.gameObject);
                Main.HERO_DIED();
            }
        }
    }

    Weapon GetEmptyWeaponSlot(){
        for(int i = 0; i<weapons.Length; i++){
            if(weapons[i].type == eWeaponType.none){
                return(weapons[i]);
            }
        }
        return(null);
    }

    void ClearWeapons(){
        foreach(Weapon w in weapons){
            w.SetType(eWeaponType.none);
        }
    }
    IEnumerator TempInvincibility() {
        isInvincible = true;
        Debug.Log("Invincible!");

        float blinkDuration = 7f;
        float blinkRate = 0.1f;
        float timer = 0f;
        bool showBlink = false;

        while (timer < blinkDuration) {
            showBlink = !showBlink;

            for (int i = 0; i < heroMaterials.Length; i++) {
                heroMaterials[i].color = showBlink ? blinkColor : originalColors[i];
            }

            yield return new WaitForSeconds(blinkRate);
            timer += blinkRate;
        }

        // Reset to normal
        for (int i = 0; i < heroMaterials.Length; i++) {
            heroMaterials[i].color = originalColors[i];
        }

        isInvincible = false;
        Debug.Log("Back to normal");
    }
}
