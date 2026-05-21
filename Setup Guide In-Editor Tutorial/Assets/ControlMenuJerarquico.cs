using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class ControlMenuJerarquico : MonoBehaviour
{
    [Header("Configuración de Paneles")]
    public GameObject pantallaInicio;
    public GameObject panelPrincipal;
    public GameObject[] subPaneles; 
    
    [Header("Configuración de Botones")]
    public Button[] botonesPrincipales; 
    public RectTransform selector;
    
    [Header("Configuración Red (MATLAB)")]
    public int puertoEscucha = 7000;
    UdpClient client;
    Thread receiveThread;
    string lastReceivedUDPPacket = "";

    [Header("Configuración de Audios")]
    public AudioSource emisorSonido;
    public AudioClip[] audiosPrincipales; // 6 sonidos: Necesidades, Salud, Social, Ocio, Asistencia, Ayuda
    public AudioClip[] audiosNecesidades;
    public AudioClip[] audiosSalud;
    public AudioClip[] audiosSocial;
    public AudioClip[] audiosOcio;
    public AudioClip[] audiosAsistencia;
    public AudioSource sonidoIntro;
    
    private AudioClip[] audiosSubpanelActual; // Esta es la "lista activa" en cada momento

    private int indiceActual = 0;
    private int panelActivoIndex = -1; 
    private Button[] botonesActivos;
    private GameObject panelActual; 
    private bool appEnLaPortada = true;

    void Start()
    {
        // 1. Configuración de Interfaz
        if(pantallaInicio != null) pantallaInicio.SetActive(true);
        if(panelPrincipal != null) panelPrincipal.SetActive(false);
        foreach(GameObject p in subPaneles) { if(p != null) p.SetActive(false); }

        botonesActivos = botonesPrincipales;
        ActualizarSelector();

        // 2. Iniciar UDP
        client = new UdpClient(puertoEscucha);
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    private void ReceiveData()
    {
        while (true)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP);
                lastReceivedUDPPacket = Encoding.UTF8.GetString(data);
            }
            catch (System.Exception e) { Debug.Log(e.ToString()); }
        }
    }

    void Update()
    {
        if (lastReceivedUDPPacket != "")
        {
            string mensaje = lastReceivedUDPPacket;
            lastReceivedUDPPacket = "";

            if (mensaje == "1") MoverSiguiente();
            else if (mensaje == "2") MoverAnterior();
            else if (mensaje == "3")
            {
                if (appEnLaPortada) EntrarALaApp();
                else Seleccionar();
            }
        }

        // Teclado para pruebas
        if (appEnLaPortada)
        {
            if (Input.GetKeyDown(KeyCode.Alpha3)) EntrarALaApp();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) MoverSiguiente();
        if (Input.GetKeyDown(KeyCode.Alpha2)) MoverAnterior();
        if (Input.GetKeyDown(KeyCode.Alpha3)) Seleccionar();
    }

    public void EntrarALaApp()
    {
        appEnLaPortada = false;
	if (sonidoIntro != null){
		sonidoIntro.Play();}
        if(pantallaInicio != null) pantallaInicio.SetActive(false);
        if(panelPrincipal != null) panelPrincipal.SetActive(true);
        botonesActivos = botonesPrincipales;
        indiceActual = 0;
        ActualizarSelector();
    }

    public void MoverAnterior()
    {
        if (botonesActivos == null || botonesActivos.Length == 0) return;
        indiceActual--; 
        if (indiceActual < 0) indiceActual = botonesActivos.Length - 1;
        ActualizarSelector();
    }

    public void MoverSiguiente()
    {
        if (botonesActivos == null || botonesActivos.Length == 0) return;
        indiceActual = (indiceActual + 1) % botonesActivos.Length;
        ActualizarSelector();
    }

   public void Seleccionar()
{
    // CORTAR el sonido que esté sonando actualmente
    if (emisorSonido.isPlaying) 
    {
        emisorSonido.Stop();
    }

    if (panelActivoIndex == -1) // MENÚ PRINCIPAL
    {
        if (indiceActual < audiosPrincipales.Length && audiosPrincipales[indiceActual] != null)
        {
            // Ahora este sonará desde el silencio
            emisorSonido.PlayOneShot(audiosPrincipales[indiceActual]);
        }

        if (indiceActual == 5) { EjecutarGritoAyuda(); }
        else { AbrirSubpanel(indiceActual); }
    }
    else // DENTRO DE UN SUBPANEL
    {
        if (audiosSubpanelActual != null && indiceActual < audiosSubpanelActual.Length && audiosSubpanelActual[indiceActual] != null)
        {
            // Ahora este sonará desde el silencio
            emisorSonido.PlayOneShot(audiosSubpanelActual[indiceActual]);
        }

        string nombreBoton = botonesActivos[indiceActual].name.ToUpper();
        if (nombreBoton.Contains("VOLVER")) { VolverAlPrincipal(); }
        else { Debug.Log("Acción ejecutada en subpanel"); }
    }
}

    void EjecutarGritoAyuda() { Debug.Log("¡¡AYUDA!!"); }
    
    public void AbrirSubpanel(int index)
    {
        if (index >= subPaneles.Length || subPaneles[index] == null) return;
        
        panelPrincipal.SetActive(false);
        panelActivoIndex = index;
        panelActual = subPaneles[index];
        panelActual.SetActive(true);
        
        // Actualizamos los botones activos del nuevo panel
        botonesActivos = panelActual.GetComponentsInChildren<Button>();
        indiceActual = 0; 

        // ASIGNAMOS LA LISTA DE AUDIOS CORRESPONDIENTE
        switch(index)
        {
            case 0: audiosSubpanelActual = audiosNecesidades; break;
            case 1: audiosSubpanelActual = audiosSalud; break;
            case 2: audiosSubpanelActual = audiosSocial; break;
            case 3: audiosSubpanelActual = audiosOcio; break;
            case 4: audiosSubpanelActual = audiosAsistencia; break;
        }

        ActualizarSelector();
    }

    public void VolverAlPrincipal()
    {
        if (panelActual != null) panelActual.SetActive(false);
        panelPrincipal.SetActive(true);
        botonesActivos = botonesPrincipales;
        panelActivoIndex = -1;
        audiosSubpanelActual = null; // Limpiamos la lista al volver
        indiceActual = 0;
        ActualizarSelector();
    }

    void ActualizarSelector()
    {
        if (botonesActivos != null && botonesActivos.Length > 0 && selector != null)
        {
            selector.position = botonesActivos[indiceActual].transform.position;
            RectTransform rtBoton = botonesActivos[indiceActual].GetComponent<RectTransform>();
            selector.sizeDelta = new Vector2(rtBoton.sizeDelta.x + 30, rtBoton.sizeDelta.y + 30);
        }
    }

    void OnApplicationQuit()
    {
        if (receiveThread != null) receiveThread.Abort();
        if (client != null) client.Close();
    }
}