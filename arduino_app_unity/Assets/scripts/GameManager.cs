using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using System.IO.Ports;
using TMPro;
using Newtonsoft.Json.Linq;

public class GameManager : MonoBehaviour
{
    public SerialPort main_port;
    //GŁÓWNA SZACHOWNICA
    public Board main;



    //GRA


    //POLA SZACHOWNICY INGAME
    public GameObject light_square;
    public GameObject dark_square;
    public Sprite empty_square;


    //FIGURY INGAME

    //BIAŁE
    public GameObject white_pawn;
    public GameObject white_knight;
    public GameObject white_bishop;
    public GameObject white_rook;
    public GameObject white_queen;
    public GameObject white_king;


    //CZARNE
    public GameObject black_pawn;
    public GameObject black_knight;
    public GameObject black_bishop;
    public GameObject black_rook;
    public GameObject black_queen;
    public GameObject black_king;


    //TABLICA ZAWEIERAJĄCA PREFABY INGAME INICJALIZOWANA W STARCIE
    public GameObject[] white_templates;
    public GameObject[] black_templates;




    //EDYTOR


    //AKTYWNA GRAFIKA EDYTORA
    public Sprite active_sprite;

    //PREFABY PÓL EDYTORA
    public GameObject light_square_template;
    public GameObject dark_square_template;


    //PREFABY EDYTORA

    //BIAŁE
    public GameObject white_pawn_template;
    public GameObject white_knight_template;
    public GameObject white_bishop_template;
    public GameObject white_rook_template;
    public GameObject white_queen_template;
    public GameObject white_king_template;


    //CZANE
    public GameObject black_pawn_template;
    public GameObject black_knight_template;
    public GameObject black_bishop_template;
    public GameObject black_rook_template;
    public GameObject black_queen_template;
    public GameObject black_king_template;


    //KOSZ
    public GameObject bin;


    // PUSTE POLE EDYTORA
    public GameObject empty_piece_prefab;




    //PREFABY DO PROMOWANIA FIGUR

    //BIAŁE
    public GameObject white_knight_promotion;
    public GameObject white_bishop_promotion;
    public GameObject white_rook_promotion;
    public GameObject white_queen_promotion;



    //CZARNE
    public GameObject black_knight_promotion;
    public GameObject black_bishop_promotion;
    public GameObject black_rook_promotion;
    public GameObject black_queen_promotion;


    public int confident_bound_check = 0;
    public float arduino_check_timer = 0.01f;
    public int confident_ticks = 100;


    //Global editor flags
    public bool K = true;
    public bool Q = true;
    public bool k = true;
    public bool q = true;
    public bool whitemoveeditor = true;
    public string enpassant_editor_value;

    //ulong aktulany stan gry
    public ulong official_bitboard;

    //dots

    public GameObject white_dot;
    public GameObject black_dot;

    public int time;
    public int increment;

    public void setIncrement(string val)
    {
        increment = int.Parse(val);
    }
    public void setTime(string val)
    {
        time = int.Parse(val)*60;
    }

    public void updateFlags()
    {
        K = Ktoggle.isOn == true ? true : false;
        Q = Qtoggle.isOn == true ? true : false;
        k = ktoggle.isOn == true ? true : false;
        q = qtoggle.isOn == true ? true : false;
        whitemoveeditor = whitemove.isOn == true ? true : false;
    }

    //UI

    public TMP_InputField generated_Fen;

    public UnityEngine.UI.Toggle Ktoggle;
    public UnityEngine.UI.Toggle Qtoggle;
    public UnityEngine.UI.Toggle ktoggle;
    public UnityEngine.UI.Toggle qtoggle;
    public UnityEngine.UI.Toggle whitemove;

    public GameObject editor_fields;
    public GameObject ingame_fields;

    //INICJALIZACJA API
    private ApiClient _apiPlayer1;
    private ApiClient _apiPlayer2;

    //Funkcje API

    //Wyświetla wszystkie wyzwania
    //Działa!
    private async Task<string> ApiListChallengesP1()
    {
        string response = await _apiPlayer1.ApiGet("/api/challenge");
        return response;
    }
    private async Task<string> ApiListChallengesP2()
    {
        string response = await _apiPlayer2.ApiGet("/api/challenge");
        return response;
    }

    //Pobiera url ostatniego meczu
    private async Task<string> ApiGetLastOnlineGameId()
    {
        string response = await _apiPlayer1.ApiGet("/api/user/Gracz_1");
        JObject json = JObject.Parse(response);
        string url = (string)json["playing"];
        if(url == null || url == "")
        {
            return "";
        }
        string[] parts = url.Split('/');
        string gameId = parts[parts.Length - 2];
        return gameId;
    }



    //Tworzy nowe wyzwanie gracz1 -> gracz2, przyjmuje parametry:
    //clockLimit -> ilość czasu startowe w sekundach
    //clockIncrement -> ilość czasu inkrementowana po ruchu
    //!!!! UWAGA !!!!
    //gracz1 zawsze jest białymi
    //Działa!
    private async Task<string> ApiCreateChallenge(int clockLimit = 600, int clockIncrement = 0, string fen = starting_FEN)
    {
        //string endpoint = $"/api/challenge/Gracz_2?rating=false&clock.limit={clockLimit}&clock.increment={clockIncrement}&color=white&variant=standard";
        string endpoint = "/api/challenge/Gracz_2";
        var formData = new Dictionary<string, string>
        {
            { "rated", "false" },
            { "clock.limit", clockLimit.ToString() },
            { "clock.increment", clockIncrement.ToString() },
            { "color", "white" },
            { "variant", "standard" },
            { "fen", fen }
        };
        string response = await _apiPlayer1.ApiPost(endpoint, formData);
        return response;
    }

    //Anuluje wysłane wyzwanie o podanym challengeID (gracz1 stworzył i gracz1 anuluje)
    //Działa!
    private async Task<string> ApiCancelChallenge(string challengeId)
    {
        string endpoint = $"/api/challenge/{challengeId}/cancel";
        string response = await _apiPlayer1.ApiPost(endpoint);
        return response;
    }

    //Akceptuje wyzwanie gracza1
    //Działa!
    private async Task<string> ApiAcceptChallenge(string challengeId)
    {
        string endpoint = $"/api/challenge/{challengeId}/accept";
        string response = await _apiPlayer2.ApiPost(endpoint);
        return response;
    }

    //Odrzuca wyzwanie gracz1
    //Działa!
    private async Task<string> ApiDeclineChallenge(string challengeId)
    {
        string endpoint = $"/api/challenge/{challengeId}/decline";
        string response = await _apiPlayer2.ApiPost(endpoint);
        return response;
    }

    //Dodaje czas określoną ilość sekund przeciwnikowi
    //Działa!
    private async Task<string> ApiAddTimeToP1(string gameId, int seconds)
    {
        string endpoint = $"/api/round/{gameId}/add-time/{seconds}";
        string response = await _apiPlayer2.ApiPost(endpoint);
        return response;
    }
    private async Task<string> ApiAddTimeToP2(string gameId, int seconds)
    {
        string endpoint = $"/api/round/{gameId}/add-time/{seconds}";
        string response = await _apiPlayer1.ApiPost(endpoint);
        return response;
    }

    //Wyślij nowy ruch do Lichess
    //move zapisywany jest w formacie UCI -> e2e4, e3e4 itd.
    //Działa!
    private async Task<string> ApiMakeMoveP1(string gameId, string move)
    {
        string endpoint = $"/api/board/game/{gameId}/move/{move}";
        string response = await _apiPlayer1.ApiPost(endpoint);
        return response;
    }
    private async Task<string> ApiMakeMoveP2(string gameId, string move)
    {
        string endpoint = $"/api/board/game/{gameId}/move/{move}";
        string response = await _apiPlayer2.ApiPost(endpoint);
        return response;
    }

    //Zrezygnuj z gry (Resign a game)
    private async Task<string> ApiResignP1(string gameId)
    {
        string endpoint = $"/api/board/game/{gameId}/resign";
        string response = await _apiPlayer1.ApiPost(endpoint);
        return response;
    }
    private async Task<string> ApiResignP2(string gameId)
    {
        string endpoint = $"/api/board/game/{gameId}/resign";
        string response = await _apiPlayer2.ApiPost(endpoint);
        return response;
    }

    //Zaproponuj, zaakceptuj, odrzuć remis (true - zaakceptuj/zaproponuj, false - odrzuć)
    private async Task<string> ApiHandleDrawP1(string gameId, bool accept)
    {
        string endpoint = $"/api/board/game/{gameId}/draw/{accept}";
        string response = await _apiPlayer1.ApiPost(endpoint);
        return response;
    }
    private async Task<string> ApiHandleDrawP2(string gameId, bool accept)
    {
        string endpoint = $"/api/board/game/{gameId}/draw/{accept}";
        string response = await _apiPlayer2.ApiPost(endpoint);
        return response;
    }

    //Zaproponuj, zaakceptuj, odrzuć cofnięcie ruchu (true - zaakceptuj/zaproponuj, false - odrzuć)
    //Działa!
    private async Task<string> ApiHandleTakebackP1(string gameId, bool accept)
    {
        string endpoint = $"/api/board/game/{gameId}/takeback/{accept}";
        string response = await _apiPlayer1.ApiPost(endpoint);
        return response;
    }
    private async Task<string> ApiHandleTakebackP2(string gameId, bool accept)
    {
        string endpoint = $"/api/board/game/{gameId}/takeback/{accept}";
        string response = await _apiPlayer2.ApiPost(endpoint);
        return response;
    }

    //napisz na czacie
    //room -> enum: "player" / "spectator"
    //Działa!
    private async Task<string> ApiSendMessageFromP1(string gameId, string text, string room = "spectator")
    {
        string endpoint = $"/api/board/game/{gameId}/chat";
        var formData = new Dictionary<string, string>
        {
            { "text", text },
            { "room", room }
        };
        string response = await _apiPlayer1.ApiPost(endpoint, formData);
        return response;
    }
    private async Task<string> ApiSendMessageFromP2(string gameId, string text, string room = "spectator")
    {
        string endpoint = $"/api/board/game/{gameId}/chat";
        var formData = new Dictionary<string, string>
        {
            { "text", text },
            { "room", room }
        };
        string response = await _apiPlayer2.ApiPost(endpoint, formData);
        return response;
    }


    public string starting_fen;
    public string challangeID = "";
    public bool OpenBrowser = true;
    void Start()
    {
        white_templates = new GameObject[]{
        white_pawn,
        white_knight,
        white_bishop,
        white_rook,
        white_queen,
        white_king
        };
        black_templates = new GameObject[]{
        black_pawn,
        black_knight,
        black_bishop,
        black_rook,
        black_queen,
        black_king
        };


        //INICJALIZACJA GŁÓWNEJ SZACHOWNICY
        main = new Board(GetComponent<GameManager>(), new Vector2(-4.5f, -4));
        main.create_from_FEN(starting_fen);
        main.CreateVisualFromBoard();
        main_string = Convert.ToString(main.all_pieces_bitboard);
        temp = main_string;
        // Debug.Log(main.Perft(6,6));
        // Debug.Log(main.counter);
        //StartCoroutine(main.PerftVisual(3));
        // StartCoroutine(readPort());
        StartCoroutine(TryDetectCOM());
        // Ktoggle = GameObject.Find("Kingside").GetComponent<UnityEngine.UI.Toggle>();
        // Qtoggle = GameObject.Find("Queenside").GetComponent<UnityEngine.UI.Toggle>();
        // ktoggle = GameObject.Find("kingside").GetComponent<UnityEngine.UI.Toggle>();
        // qtoggle = GameObject.Find("queenside").GetComponent<UnityEngine.UI.Toggle>();

        //API
        _apiPlayer1 = new ApiClient("lip_IRCrZrvSikTzlTdm4YMz");
        _apiPlayer2 = new ApiClient("lip_3riSzZhsK0kGFZ3oT6Dj");


    }


    public Coroutine reading;

    public void start_syncing()
    {
        if(reading != null)
        {
            return;
        }
        main.DestroyVisual();
        main.CreateVisualFromBoard();
        main_string = Convert.ToString(main.all_pieces_bitboard);
        temp=main_string;
        string fenlichess = GenerateFEN_from_array(genreate_array_for_fen());
        if(main.white_move)
        {
            white_dot.SetActive(true);
            black_dot.SetActive(false);
        }else
        {
            white_dot.SetActive(false);
            black_dot.SetActive(true);
        }
        Task.Run(async () =>
        {
            Debug.Log("API LOG: \n List P1:");
            while (true)
            {
                try
                {
                    string res = await ApiGetLastOnlineGameId();
                    if(res == "")
                    {
                        break;
                    }
                    Debug.Log(await ApiResignP1(res));
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                };
            }
            
            string response = await ApiCreateChallenge(time, increment, fenlichess);
            Debug.Log(response);
            JObject json = JObject.Parse(response);
            JObject challenge = (JObject)json["challenge"];
            string challengeId = (string)challenge["id"];
            challangeID = challengeId;
            response = await ApiAcceptChallenge(challengeId);
            Debug.Log(response);
            if(OpenBrowser)
                System.Diagnostics.Process.Start($"https://www.lichess.org/{challangeID}");
        });
        reading = StartCoroutine(readPort());
    }

    void Update()
    {

        //CZEKANIE NA COFNIĘCIE
        if (Input.GetKeyDown(KeyCode.W))
        {
            takeback();
        }
    }

    public void go_to_editor()
    {
        GameObject.Find("Main Camera").transform.position+=new Vector3(25,0,0);
        editor_fields.SetActive(true);
        ingame_fields.SetActive(false);
    }

    public void go_to_game()
    {
        GameObject.Find("Main Camera").transform.position+=new Vector3(-25,0,0);
        editor_fields.SetActive(false);
        ingame_fields.SetActive(true);
    }
    public void takeback()
    {
        if (main.history_index.Count > 0 && main.history_index.Count != 0)
        {
            main.undo_move(main.history_index[main.history_index.Count - 1]);
            if(main.white_move)
            {
                white_dot.SetActive(true);
                black_dot.SetActive(false);
            }else
            {
                white_dot.SetActive(false);
                black_dot.SetActive(true);
            }
            generated_Fen.text = GenerateFEN_from_array(genreate_array_for_fen());
            main.DestroyVisual();
            main.CreateVisualFromBoard();
        }
    }
    //FUNKCJE API


    //FUNKCJE ARDUINO
    public IEnumerator TryDetectCOM()
    {
        string[] ports = SerialPort.GetPortNames();
        bool czyZnaleziono = false;
        foreach (string s in ports)
        {
            Debug.Log(s);
        }
        if (ports.Count() > 0)
        {
            foreach (string port in ports)
            {
                main_port = new SerialPort(port, 115200);
                try
                {
                    main_port.Open();
                    main_port.Write("chessACK");
                    Debug.Log("Otwarto port " + port);
                    if (main_port.ReadLine() != "")
                    {
                        Debug.Log("Arduino znaleziono na porcie: " + port);
                        czyZnaleziono = true;
                        break;
                    }
                }
                catch
                {
                    Debug.Log("Port: " + port + " not open!");
                }
            }
        }
        if(!czyZnaleziono) 
            Debug.LogError("Nie znaleziono żadnego urządzenia!!!");
        yield return null;
    }//chodzi mi o to że trzeba coś wysłać ono music odebrać i wysłać coś z powrotem



    public bool first_set = false;
    public int potential_first = 0, potential_second = 0;
    public float confidence_time = 0.5f;
    public float time_irl = 0;
    public bool prepare_for_move_make = true;
    
    public string now;
    public string temp;
    public string main_string;
    public IEnumerator readPort()
    {
        Debug.Log("uruchomiono");
        main_port.DiscardInBuffer();
        while (true)
        {
            now = main_port.ReadLine();
            if(now == main_string)
            {
                time_irl = 0;
                first_set = false;
            }
            if (now == temp && prepare_for_move_make)
            {
                time_irl += Time.deltaTime;
                if(time_irl > confidence_time)
                {
                    prepare_for_move_make = false;
                    if(TryToMakeMoveFromIrl(potential_first,potential_second))
                    {
                        main_string = now;
                        first_set = false;
                        if(main.MoveGen().Count == 0)
                        {
                            stopReading(true);
                        }
                    }
                    time_irl=0;
                }
            }
            else 
            {
                time_irl = 0;
                prepare_for_move_make = false;
                ulong result = 0;
                try
                {
                    ulong new_board = UInt64.Parse(now);
                    ulong old_board = UInt64.Parse(temp);
                    result = old_board ^ new_board;
                    for(int i=0; i<=63; i++)
                    {
                        if((1UL<<i & result) == 1UL<<i)
                        {
                            if((new_board & 1UL<<i) == 1UL<<i)  //postawienie
                            {
                                if(potential_first == i)
                                {
                                    continue;
                                }
                                potential_second = i;
                                prepare_for_move_make = true;
                                temp = now;
                            }else //wziecie
                            {
                                if(!first_set)
                                {
                                    if(main.board[i].piece != null)
                                    {
                                        if(main.board[i].piece.color==1 && main.white_move || main.board[i].piece.color == -1 && !main.white_move)
                                        {
                                            potential_first = i;
                                            first_set = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    // for (int i = 0; i <= 63; i++)
                    // {
                    //     if((result & 1UL<<i) == 1UL<<i)
                    //     {
                    //         if(!first_set)
                    //         {
                    //             potential_first = i;
                    //             first_set = true;
                    //         }else
                    //         {
                    //             if(potential_first == i)
                    //             {
                    //                 continue;
                    //             }
                    //             potential_second = i;
                    //             prepare_for_move_make = true;
                    //             temp = now;
                    //         }
                    //     }
                    // }
                    temp=now;
                }
                catch
                {
                    Debug.Log(result);
                    Debug.Log("zły tekst");
                }
            }
            yield return null;
            //yield return new WaitForSeconds(arduino_check_timer);
        }
    }

    public void stopReading()
    {
        if(reading == null)
        {
            return;
        }
        white_dot.SetActive(false);
        black_dot.SetActive(false);
        StopCoroutine(reading);
        reading = null;
        if (main.white_move)
        {
            Task.Run(async () =>
            {
                Debug.Log("Cancel meczu");
                string response = await ApiResignP1(challangeID);
                Debug.Log(response);
            });
        }
        else
        {
            Task.Run(async () =>
            {
                Debug.Log("Cancel meczu");
                string response = await ApiResignP2(challangeID);
                Debug.Log(response);
            });
        }
        
    }

    public void stopReading(bool checkmate = true)
    {
        if (reading == null)
        {
            return;
        }
        white_dot.SetActive(false);
        black_dot.SetActive(false);
        StopCoroutine(reading);
        reading = null;
    }
    public Coroutine is_promoting = null; 
    public bool TryToMakeMoveFromIrl(int st, int tar)
    {
        List<Move> potential = main.MoveGen();
        foreach(Move m in potential)
        {
            Debug.Log(main.convertIndexToFieldName(m.start_square) + " -> " + main.convertIndexToFieldName(m.target_square));
        }
        foreach (Move m in potential)
        {
            if ((m.start_square == st && m.target_square == tar) || (m.start_square == tar && m.target_square == st))
            {
                if (main.board[m.start_square].piece.type == 1 && m.target_square / 8 == 7)
                {
                    if(is_promoting == null)
                    {
                        is_promoting=StartCoroutine(promotion_coroutine(1, m.start_square, m.target_square, potential));
                        break;
                    }
                }
                else if (main.board[m.start_square].piece.type == 1 && m.target_square / 8 == 0)                      
                {
                    if(is_promoting == null)
                    {
                        is_promoting=StartCoroutine(promotion_coroutine(-1, m.start_square, m.target_square, potential));
                        break;
                    }
                }
                else
                {
                Debug.Log("Oddałoby się ruszyć z: " + m.start_square);
                string move = main.convertIndexToFieldName(m.start_square) + main.convertIndexToFieldName(m.target_square);
                Debug.Log(move);
                if(main.white_move)
                {
                    white_dot.SetActive(false);
                    black_dot.SetActive(true);
                    Task.Run(async () =>
                    {
                        Debug.Log("Lichess:");
                        string response = await ApiMakeMoveP1(challangeID, move);
                        Debug.Log(response);
                    });
                    
                }
                else
                {
                    white_dot.SetActive(true);
                    black_dot.SetActive(false);
                    Task.Run(async () =>
                    {
                        Debug.Log("Lichess:");
                        string response = await ApiMakeMoveP2(challangeID, move);
                        Debug.Log(response);
                    });
                }
                main.make_move(m);
                main.DestroyVisual();
                main.CreateVisualFromBoard();
                }
                return true;
            }
        }
        return false;
    }





    //UI

    public int[] genreate_array_for_fen()
    {
        int[] temp = new int[64];
        for (int i = 0; i <= 63; i++)
        {
            if (main.board[i].piece != null)
            {
                temp[i] = main.board[i].piece.type * main.board[i].piece.color;
            }
        }
        return temp;
    }


    //FUNKCJE EDYTOR
    public void change_enpassant_editor(string change)
    {
        enpassant_editor_value = change;
    }
    public void Create_Main_From_Fen(string fen)
    {
        if (GenerateFEN_from_array(genreate_array_for_fen()) == fen)
        {
            return;
        }
        main.create_from_FEN(fen);
        main.DestroyVisual();
        main.CreateVisualFromBoard();
    }

    string GenerateFEN_from_array(int[] board)
    {
        StringBuilder fen = new StringBuilder();

        for (int row = 7; row >= 0; row--)
        {
            int emptyCount = 0;
            for (int col = 0; col < 8; col++)
            {
                int piece = board[row * 8 + col];
                if (piece == 0)
                {
                    emptyCount++;
                }
                else
                {
                    if (emptyCount > 0)
                    {
                        fen.Append(emptyCount);
                        emptyCount = 0;
                    }
                    fen.Append(PieceToSymbol(piece));
                }
            }
            if (emptyCount > 0)
            {
                fen.Append(emptyCount);
            }
            if (row > 0)
            {
                fen.Append('/');
            }
        }
        if (main.white_move)
        {
            fen.Append(" w "); // Dodanie pozostałych informacji do FEN
        }
        else
        {
            fen.Append(" b ");
        }
        bool anyCastling = false;
        if (main.board[7].piece != null && main.white_pieces[(int)piece_type.king - 1][0].moved <= 0 && main.board[7].piece.moved <= 0 && main.board[7].piece.type == (int)piece_type.rook && main.board[7].piece.color == 1)
        {
            fen.Append("K");
            anyCastling = true;
        }
        if (main.board[0].piece != null && main.white_pieces[(int)piece_type.king - 1][0].moved <= 0 && main.board[0].piece.moved <= 0 && main.board[0].piece.type == (int)piece_type.rook && main.board[0].piece.color == 1)
        {
            fen.Append("Q");
            anyCastling = true;
        }
        if (main.board[63].piece != null && main.black_pieces[(int)piece_type.king - 1][0].moved <= 0 && main.board[63].piece.moved <= 0 && main.board[63].piece.type == (int)piece_type.rook && main.board[63].piece.color == -1)
        {
            fen.Append("k");
            anyCastling = true;
        }
        if (main.board[56].piece != null && main.black_pieces[(int)piece_type.king - 1][0].moved <= 0 && main.board[56].piece.moved <= 0 && main.board[56].piece.type == (int)piece_type.rook && main.board[56].piece.color == -1)
        {
            fen.Append("q");
            anyCastling = true;
        }
        if (!anyCastling)
        {
            fen.Append("-");
        }


        if (main.enpassant_square > 0)
        {
            fen.Append(" ");
            fen.Append(main.convertIndexToFieldName(main.enpassant_square));
        }
        else
        {
            fen.Append(" -");
        }


        fen.Append(" 0 1");
        Debug.Log(fen);
        generated_Fen.text = fen.ToString();
        return fen.ToString();
    }


    string GenerateFEN_from_array(int[] board, bool editor)
    {
        StringBuilder fen = new StringBuilder();

        for (int row = 7; row >= 0; row--)
        {
            int emptyCount = 0;
            for (int col = 0; col < 8; col++)
            {
                int piece = board[row * 8 + col];
                if (piece == 0)
                {
                    emptyCount++;
                }
                else
                {
                    if (emptyCount > 0)
                    {
                        fen.Append(emptyCount);
                        emptyCount = 0;
                    }
                    fen.Append(PieceToSymbol(piece));
                }
            }
            if (emptyCount > 0)
            {
                fen.Append(emptyCount);
            }
            if (row > 0)
            {
                fen.Append('/');
            }
        }
        if (whitemoveeditor)
        {
            fen.Append(" w "); // Dodanie pozostałych informacji do FEN
        }
        else
        {
            fen.Append(" b ");
        }
        bool anyCastling = false;
        if (K)
        {
            fen.Append("K");
            anyCastling = true;
        }
        if (Q)
        {
            fen.Append("Q");
            anyCastling = true;
        }
        if (k)
        {
            fen.Append("k");
            anyCastling = true;
        }
        if (q)
        {
            fen.Append("q");
            anyCastling = true;
        }
        if (!anyCastling)
        {
            fen.Append("-");
        }

        List<char> firstLetter = new List<char>() { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' };
        List<char> secondNumber = new List<char>() { '3', '6', };
        if (enpassant_editor_value.Length == 2)
        {
            if (firstLetter.Contains(enpassant_editor_value[0]) && secondNumber.Contains(enpassant_editor_value[1]))
            {
                fen.Append(" ");
                fen.Append(enpassant_editor_value);
            }
            else
            {
                fen.Append(" -");
            }
        }
        else
        {
            fen.Append(" -");
        }



        fen.Append(" 0 1");
        Debug.Log(fen);
        generated_Fen.text = fen.ToString();
        return fen.ToString();
    }

    static char PieceToSymbol(int piece)
    {
        switch (piece)
        {
            case 1: return 'P'; // Pionek
            case 2: return 'N'; // Skoczek
            case 3: return 'B'; // Goniec
            case 4: return 'R'; // Wieża
            case 5: return 'Q'; // Królowa
            case 6: return 'K'; // Król
            case -1: return 'p'; // Pionek
            case -2: return 'n'; // Skoczek
            case -3: return 'b'; // Goniec
            case -4: return 'r'; // Wieża
            case -5: return 'q'; // Królowa
            case -6: return 'k'; // Król
            default: throw new ArgumentException("Niepoprawna wartość pionka");
        }
    }

    public void generate_board_from_editor()
    {
        main.create_from_FEN(GenerateFEN_from_array(main.editor_state, true));
        main.DestroyVisual();
        main.CreateVisualFromBoard();
    }

    public void generate_starting_position()
    {
        main.create_from_FEN(starting_FEN);
        main.DestroyVisual();
        main.CreateVisualFromBoard();
    }

    //FEN DEFAULTOWY - SZACHOWNICA NA START 
    const string starting_FEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";


    // PIECE_TYPE to jest typ służący do opisu typu figury (piece.type) zakres wartości 1-6 pionek,koń...król kolor określa wartość (piece.color) type odpowiada wyłącznie za rodzaj ; 0 określa brak figury
    enum piece_type { pawn = 1, knight, bishop, rook, queen, king };


    //Pole kliknięte przez gracza
    int start_move = -1;

    //Licznik powtórzeń
    public int repeat_counter = 0;

    //W przypadku promocji jaki rodzaj pionka wybierasz
    public int type_of_promotion;

    List<int> indexes_to_check = new List<int>();

    //Odebranie ruchu od pola
    public void recieveField(int sq)
    {
        if (start_move < 0)
        {
            start_move = sq;
            List<Move> moves = main.MoveGen();
            indexes_to_check.Clear();
            foreach (Move m in moves)
            {
                if (m.start_square == sq)
                {
                    indexes_to_check.Add(m.target_square);
                }
            }
            for (int i = 0; i <= 63; i++)
            {
                if (indexes_to_check.Contains(main.ingame_fields[i].GetComponent<collideScript>().sq))
                {
                    main.ingame_fields[i].GetComponent<SpriteRenderer>().sprite = empty_square;
                }
            }
        }
        else
        {
            List<Move> moves = main.MoveGen();
            foreach (Move m in moves)
            {
                Debug.Log(main.convertIndexToFieldName(m.start_square) + " -> " + main.convertIndexToFieldName(m.target_square));
            }
            for (int i = 0; i <= 63; i++)
            {
                if (indexes_to_check.Contains(main.ingame_fields[i].GetComponent<collideScript>().sq))
                {
                    main.ingame_fields[i].GetComponent<SpriteRenderer>().sprite = main.ingame_fields[i].GetComponent<collideScript>().default_sprite;
                }
            }
            foreach (Move m in moves)
            {
                if (m.start_square == start_move && m.target_square == sq)
                {
                    if (main.board[start_move].piece.type == 1 && sq / 8 == 7)
                    {
                        if(is_promoting == null)
                        {
                            is_promoting=StartCoroutine(promotion_coroutine(1, start_move, sq, moves));
                            break;
                        }
                    }
                    else if (main.board[start_move].piece.type == 1 && sq / 8 == 0)
                    {
                        if(is_promoting == null)
                        {
                            is_promoting=StartCoroutine(promotion_coroutine(-1, start_move, sq, moves));
                            break;
                        }
                    }
                    else
                    {
                        TryToMakeMoveFromIrl(m.start_square,m.target_square);
                        // main.make_move(m);
                        generated_Fen.text = GenerateFEN_from_array(genreate_array_for_fen());
                        List<int> temp = new List<int>();
                        foreach (Square s in main.board)
                        {
                            if (s.piece != null)
                            {
                                temp.Add(s.piece.type * s.piece.color);
                            }
                            else
                            {
                                temp.Add(0);
                            }
                        }
                        main.three_move_history.Add(temp);
                        repeat_counter = 0;
                        foreach (List<int> pos in main.three_move_history)
                        {
                            bool is_valid = true;
                            for (int i = 0; i <= pos.Count - 1; i++)
                            {
                                if (temp[i] != pos[i])
                                {
                                    is_valid = false;
                                    break;
                                }
                            }
                            if (is_valid)
                            {
                                repeat_counter += 1;
                            }
                        }
                        if (repeat_counter >= 3)
                        {
                            Debug.Log("Powtorzenie pozycji");
                        }
                        main.DestroyVisual();
                        main.CreateVisualFromBoard();
                    }
                    break;
                }
            }
            start_move = -1;
        }
    }

    //Korutyna czekająca na wybór promocji
    public IEnumerator promotion_coroutine(int color, int start, int target, List<Move> temp_moves)
    {
        Debug.Log("corutyna promuje");
        List<GameObject> temp_promotion = new List<GameObject>();
        if (color == 1)
        {
            temp_promotion.Add(Instantiate(white_knight_promotion, new Vector3(-10f, 4f, 0), Quaternion.identity));
            temp_promotion.Add(Instantiate(white_bishop_promotion, new Vector3(-10f, 2f, 0), Quaternion.identity));
            temp_promotion.Add(Instantiate(white_rook_promotion, new Vector3(-10f, 0f, 0), Quaternion.identity));
            temp_promotion.Add(Instantiate(white_queen_promotion, new Vector3(-10f, -2f, 0), Quaternion.identity));
        }
        else
        {
            temp_promotion.Add(Instantiate(black_knight_promotion, new Vector3(-10f, 4f, 0), Quaternion.identity));
            temp_promotion.Add(Instantiate(black_bishop_promotion, new Vector3(-10f, 2f, 0), Quaternion.identity));
            temp_promotion.Add(Instantiate(black_rook_promotion, new Vector3(-10f, 0f, 0), Quaternion.identity));
            temp_promotion.Add(Instantiate(black_queen_promotion, new Vector3(-10f, -2f, 0), Quaternion.identity));
        }
        while (type_of_promotion == 0)
        {
            yield return null;
        }
        for (int i = 0; i <= temp_promotion.Count - 1; i++)
        {
            Destroy(temp_promotion[i]);
        }
        foreach (Move m in temp_moves)
        {
            if (m.piece_promoted != null)
                if (m.piece_promoted.type == type_of_promotion && m.target_square == target)
                {
                    //main.make_move(m);
                    // TryToMakeMoveFromIrl(m.start_square,m.target_square,type_of_promotion);



                Debug.Log("Oddałoby się ruszyć z: " + m.start_square);
                string move = main.convertIndexToFieldName(m.start_square) + main.convertIndexToFieldName(m.target_square);
                if(type_of_promotion == 2)
                {
                    move+='n';
                }
                if(type_of_promotion == 3)
                {
                    move+='b';
                }
                if(type_of_promotion == 4)
                {
                    move+='r';
                }
                if(type_of_promotion == 5)
                {
                    move+='q';
                }
                Debug.Log(move);
                if(main.white_move)
                {
                    white_dot.SetActive(false);
                    black_dot.SetActive(true);
                    Task.Run(async () =>
                    {
                        Debug.Log("Lichess:");
                        string response = await ApiMakeMoveP1(challangeID, move);
                        Debug.Log(response);
                    });
                }
                else
                {
                    white_dot.SetActive(false);
                    black_dot.SetActive(true);
                    Task.Run(async () =>
                    {
                        Debug.Log("Lichess:");
                        string response = await ApiMakeMoveP2(challangeID, move);
                        Debug.Log(response);
                    });
                }
                main.make_move(m);
                main.DestroyVisual();
                main.CreateVisualFromBoard();



                    List<int> temp = new List<int>();
                    foreach (Square s in main.board)
                    {
                        if (s.piece != null)
                        {
                            temp.Add(s.piece.type * s.piece.color);
                        }
                        else
                        {
                            temp.Add(0);
                        }
                    }
                    main.three_move_history.Add(temp);
                    repeat_counter = 0;
                    foreach (List<int> pos in main.three_move_history)
                    {
                        bool is_valid = true;
                        for (int i = 0; i <= pos.Count - 1; i++)
                        {
                            if (temp[i] != pos[i])
                            {
                                is_valid = false;
                                break;
                            }
                        }
                        if (is_valid)
                        {
                            repeat_counter += 1;
                        }
                    }
                    if (repeat_counter >= 3)
                    {
                        Debug.Log("Powtorzenie pozycji");
                    }
                    main.DestroyVisual();
                    main.CreateVisualFromBoard();
                    break;
                }
        }
        type_of_promotion = 0;
        kill_promotion_coroutine();
    }


    public void kill_promotion_coroutine()
    {
        if(is_promoting != null)
        {
            StopCoroutine(is_promoting);
            is_promoting = null;
        }
    }


    // KLASA PIECE odpowiada za figury na szachownicy - na każdym polu może być figura lub null 
    public class Piece
    {
        //PIECE_TYPE
        public int type = 0;


        // 1 - białe LUB -1 - czarne 
        public int color;

        // int określający index pola 0-63 a1-h8
        public int square;

        //ilość ruchów wykonanych figurą przydatne np. do sprawdzania legalności roszady i tego czy pionek może ruszyć się o dwa...
        public int moved = 0;


        //określane przez maskę pinowania - zobacz generate_pinned()
        public ulong pin_mask = ulong.MaxValue;

        //prosty konstruktor
        public Piece(int t, int c, int s)
        {
            type = t;
            color = c;
            square = s;
        }

    }


    public class Move
    {
        //skąd ruch
        public int start_square;

        //dokąd ruch
        public int target_square;


        // czy została zbita jakaś bierka (enpassant też się tu wlicza) jeśli tak to zbita figura w trakcie ruchu, w przeciwnym null
        public Piece piece_taken;


        // rozpatrywane tylko i wyłącznie przy promocji może tu być koń goniec wieża lub królówka
        public Piece piece_promoted;

        // zapamiętuje jaki pionek dokonał promocji żeby w przypadku cofnięcia ruchu wiedzieć jaki to pionek i gdzie stoi (wszystko zapisane m. in. w klasie figura)
        public Piece pawn_used_for_promotion;


        // flagi do roszady
        public bool is_white_castle_long;
        public bool is_white_castle_short;
        public bool is_black_castle_long;
        public bool is_black_castle_short;



        //konstruktor
        public Move(int start, int target)
        {
            start_square = start;
            target_square = target;
        }

    }


    //board składa się z 63 sqaure ; służy do przechowywania informacji o stanie szachownicy
    public class Square
    {
        //index pola 0-63 a1-h8
        public int index;

        //figura jaka stoi na danym polu
        public Piece piece;

        public Square()
        {
            piece = null;
        }
    }


    // GŁÓWNA KLASA OPISUJĄCA SZACHOWNICĘ 
    // JEST SERIALIZABLE ŻEBY MOŻNA BYŁO ZOBACZYĆ JĄ W INSPEKTORZE PO PRAWEJ
    // SKŁADA SIĘ Z KILKU WAŻNYCH RZECZY
    // - BITBOARDY - zminne 64bit służące do reprezentacji szachownicy 
    // - BOARD - lista squarów reprezentująca szachownicę razem z bb aktualizowana po ruchu
    // - MAGIC NUMBERY - liczby 64bit służące do generacji ruchów - bardzo ważne
    // - LISTY FIGUR - ułatwiają generację ruchów
    // - LISTY FIGUR INGAME - ułatwiają generację obrazu z szachownicy
    // - KILKA LIST POMOCNICZYCH - służą do generacji ruchów i mogą być raczej ignorowane
    // - DODATKOWE INFORMACJE PRZY KONKRETNYCH WŁAŚCIWOŚCIACH
    [Serializable]
    public class Board
    {
        // INGAME
        public GameManager manager;
        public Vector2 startPos;
        public List<GameObject> ingame_pieces = new List<GameObject>();
        public List<GameObject> ingame_fields = new List<GameObject>();




        //LISTA FIGUR I SZACHOWNICA 
        public List<Square> board = new List<Square>();
        public List<List<Piece>> white_pieces = new List<List<Piece>>();
        public List<List<Piece>> black_pieces = new List<List<Piece>>();
        public List<List<Piece>> all_pieces = new List<List<Piece>>();





        //LISTA BB
        public List<ulong> pieces_bitboards = new List<ulong>();
        public ulong white_pieces_bitboard = 0;
        public ulong black_pieces_bitboard = 0;
        public ulong all_pieces_bitboard = 0;




        //LISTA BB POMOCNICZYCH

        //MASKI PORUSZANIA I ATAKI PEŁNE ;; MASKA TO WSZYSTKIE POLA NA KTÓRE MOŻE RUSZYĆ SIĘ FIGURA ZAKŁADAJĄC ŻE SZACHOWNICA JEST PUSTA, POMIJAJĄC POLA SKRAJNE, USUWANIE PÓL SKRAJNYCH OMÓWIONE JEST PRZY GENEROWANIU RUCHÓW
        public List<List<ulong>> bitboard_attacks_wo_mask = new List<List<ulong>>();
        public List<ulong> rook_bitboard_mask = new List<ulong>();
        public List<ulong> bishop_bitboard_mask = new List<ulong>();
        public List<ulong> knight_bitboard_mask = new List<ulong>();
        public List<List<ulong>> rectangular_lookup = new List<List<ulong>>(); // MIERZY DYSTANS POMIĘDZY PRZYKŁADOWO DYSTANS POMIĘDZY A1 - D1 ZWRÓCI BB Z POLAMI B1 I C1 WŁĄCZONYMI


        //MAGIC NUMBERY
        List<ulong> bishop_magics = new List<ulong>() { 148619956085457442, 10403317346843297793, 16143223868717925908, 937879168757006592, 1756969072404209672, 360859724994004000, 1130650175294472, 1225544318522044416, 1567972334277633, 9260086998260023440, 17597024501760, 1697671727842816, 39600139542528, 2364531126581604352, 5769115950274054152, 550896803856, 722845474649874960, 1126037412938272, 308505576764084258, 2252970226368514, 5629508409626632, 2919881225227247616, 9231253477945118856, 1154612555646289920, 13835629806162600976, 649649214485694466, 36068426816946592, 2362156421509890304, 3531829260551462915, 2306408158192504834, 1153493528385946112, 9148495659925633, 6990730130956027008, 18300409005344808, 2311485707216093696, 288232577390084224, 4611974107653866752, 9223655994364723460, 37159112156406019, 10460738796679891968, 1298180193642416408, 72202798879868928, 327075160492361730, 9880899515320369284, 307581798116753666, 2308130027793809544, 642119639237632, 72340177149757572, 110410795216535552, 2450108010313285636, 10250216943365916160, 576478346706419978, 1297036712555446272, 36048725684585026, 594479584060129288, 297238684053342337, 162169177630573058, 1117046188556, 288232575780128769, 11542762172329493504, 144363678781833730, 36038831706888704, 576548782758756930, 2260664635752977 };
        List<ulong> rook_magics = new List<ulong>() { 1261009614724431904, 10700570857111044096, 4647732408169269378, 9979985572493791232, 720611332126277636, 72058693583372296, 324260272715858048, 72057733641142338, 144255927713808384, 2305913378495791168, 4757630862573437056, 2306124552993769504, 140754668748928, 11532029834558472704, 578149744032121344, 281510780375296, 612489824737181696, 9227875911364255748, 9227876740290838592, 23926472667299872, 300633966900674816, 864973703010584578, 4398609596952, 2289183280894212, 6918198091204083728, 54043751728808000, 13792553033793552, 9229021611066785824, 9369741224845509632, 1130300109226496, 589355414556674, 844446406054986, 11547229730218442796, 105554210983940, 281544786714632, 2305983781070180352, 46302668037030912, 288371130828325376, 2310349426406719752, 4504150490481705, 72093329239605248, 9007474141102336, 9227879072464928784, 9370020568441094176, 2305848506906116112, 562967133454464, 9817848355932602376, 5771363626854252561, 140876303368704, 54078380975325248, 9232379304963227712, 5770305742570624, 729866847994774784, 576462953474424960, 18295882078492928, 9225624115849986560, 4638725758137729057, 864972741944545409, 1154056496961552401, 1264403191870038021, 2324138900013974033, 18295907879747585, 289365145179652612, 2252934761152642 };



        //GOTOWE RUCHY Z WYKORZYSTANIEM MAGIC NUMBERÓW
        public List<List<List<ulong>>> blockers_masks = new List<List<List<ulong>>>();
        public List<List<ulong>> rook_moves = new List<List<ulong>>();
        public List<List<ulong>> bishop_moves = new List<List<ulong>>();


        //RUCHY PIONKÓW [0 - białe, 1- czarne][pole szachownicy]
        public List<List<ulong>> pawn_moves = new List<List<ulong>>();
        public List<List<ulong>> pawn_attacks = new List<List<ulong>>();



        //RUCHY KRÓLI TAKIE SAME NIEZALEŻNIE OD STRONY
        public List<ulong> king_moves = new List<ulong>();




        //WSZYSTKIE HISTORIE
        public List<Move> history_index = new List<Move>();  //RUCHÓW
        public List<int> en_passant_history = new List<int>();  //MOŻLIWOŚCI EN_PASSANT
        public List<List<int>> three_move_history = new List<List<int>>();  //POWTÓRZENIA POZYCJI





        //FLAGI POZYCJI

        public bool white_move = true;  //TRUE - ruch białych, FALSE - ruch czarnych
        public int enpassant_square = -1;  // INDEX pola na którym można w tym momencie zrobić enpassant - 0 w przypadku braku możliwości wykonania enpassant
        ulong pinned_pieces = 0;  // SŁUŻY DO SPRAWDZENIA W INSPEKTORZE JAK WYGLĄDA BB ZWIĄZANYCH FIGUR
        public ulong check_map = ulong.MaxValue; // ZAWIERA JAKIE POLA RATUJĄ OD SZACHA, BITBOARD wynosi max jeżeli nie ma szachu 
        public int fifty_move_counter = 0; // 100 halfclocków bez bicia i ruchu pionkiem - remis
        public bool draw_by_repetition_possible = false; // Sprawdza powtórzenie pozycji

        //FLAGI ROSZADY
        public bool is_white_castle_long_possible = true;
        public bool is_white_castle_short_possible = true;
        public bool is_black_castle_long_possible = true;
        public bool is_black_castle_short_possible = true;
        public int move_fen_counter;



        //EDYTOR

        public int[] editor_state = new int[64];
        public int current_editor_type;
        public int current_editor_color;
        


        //Konstruktor
        public Board(GameManager GM, Vector2 start)
        {
            //Zmienne do odwoływania się przez funkcje do ingame
            manager = GM;
            startPos = start;


            //Board init
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    Square temp = new Square();
                    temp.index = i * 8 + j;
                    board.Add(temp);
                }
            }


            //All lists init
            for (int i = 0; i < 12; i++)
            {
                all_pieces.Add(new List<Piece>());
                pieces_bitboards.Add(0);
            }

            for (int i = 0; i < 6; i++)
            {
                white_pieces.Add(new List<Piece>());
                black_pieces.Add(new List<Piece>());
            }

            //Possible moves init (magic numbers bound 2^12)
            for (int i = 0; i < 64; i++)
            {
                List<ulong> temp_rooks = new List<ulong>();
                List<ulong> temp_bishops = new List<ulong>();

                for (int j = 0; j < 4096; j++)
                {
                    temp_rooks.Add(0);
                    temp_bishops.Add(0);
                }

                rook_moves.Add(temp_rooks);
                bishop_moves.Add(temp_bishops);
            }


            //Inicjalizacja szachownicy (pola białe i czarne)
            bool isWhiteField = false;
            for (int i = 0; i <= 7; i++)
            {
                for (int j = 0; j <= 7; j++)
                {
                    if (isWhiteField)
                    {
                        GameObject g = Instantiate(manager.light_square, new Vector3(startPos.x + j * 1.28f, startPos.y + i * 1.28f, 0), Quaternion.identity);
                        g.GetComponent<collideScript>().sq = i * 8 + j;
                        g.GetComponent<collideScript>().GM = GM;
                        ingame_fields.Add(g);
                    }
                    else
                    {
                        GameObject g = Instantiate(manager.dark_square, new Vector3(startPos.x + j * 1.28f, startPos.y + i * 1.28f, 0), Quaternion.identity);
                        g.GetComponent<collideScript>().sq = i * 8 + j;
                        g.GetComponent<collideScript>().GM = GM;
                        ingame_fields.Add(g);
                    }
                    isWhiteField = !isWhiteField;
                }
                isWhiteField = !isWhiteField;
            }

            //Inicjalizacja edytora


            for (int i = 0; i <= 7; i++)
            {
                for (int j = 0; j <= 7; j++)
                {
                    if (isWhiteField)
                    {
                        //Grafiki pól
                        GameObject g = Instantiate(manager.light_square_template, new Vector3(startPos.x + j * 1.28f + 20f, startPos.y + i * 1.28f, 0), Quaternion.identity);
                        //Miejsce na figure => patrz prefab
                        g = Instantiate(manager.empty_piece_prefab, new Vector3(startPos.x + j * 1.28f + 20f, startPos.y + i * 1.28f, 0), Quaternion.identity);
                        g.GetComponent<empty_piece_editor>().index = i * 8 + j;
                    }
                    else
                    {
                        //Grafiki pól
                        GameObject g = Instantiate(manager.dark_square_template, new Vector3(startPos.x + j * 1.28f + 20f, startPos.y + i * 1.28f, 0), Quaternion.identity);
                        //Miejsce na figure => patrz prefab
                        g = Instantiate(manager.empty_piece_prefab, new Vector3(startPos.x + j * 1.28f + 20f, startPos.y + i * 1.28f, 0), Quaternion.identity);
                        g.GetComponent<empty_piece_editor>().index = i * 8 + j;
                    }
                    isWhiteField = !isWhiteField;
                }
                isWhiteField = !isWhiteField;
            }

            //Pionki aktywyjące edytor
            GameObject editor_piece = Instantiate(GM.white_pawn_template, new Vector3(26f, 3.5f, 0f), Quaternion.identity);
            editor_piece.GetComponent<template_equip>().color = 1;
            editor_piece.GetComponent<template_equip>().type = 1;
            editor_piece = Instantiate(GM.white_bishop_template, new Vector3(26f, 2f, 0f), Quaternion.identity);
            editor_piece.GetComponent<template_equip>().color = 1;
            editor_piece.GetComponent<template_equip>().type = 3;
            editor_piece = Instantiate(GM.white_knight_template, new Vector3(26f, 0.5f, 0f), Quaternion.identity);
            editor_piece.GetComponent<template_equip>().color = 1;
            editor_piece.GetComponent<template_equip>().type = 2;
            editor_piece = Instantiate(GM.white_rook_template, new Vector3(26f, -1f, 0f), Quaternion.identity);
            editor_piece.GetComponent<template_equip>().color = 1;
            editor_piece.GetComponent<template_equip>().type = 4;
            editor_piece = Instantiate(GM.white_queen_template, new Vector3(26f, -2.5f, 0f), Quaternion.identity);
            editor_piece.GetComponent<template_equip>().color = 1;
            editor_piece.GetComponent<template_equip>().type = 5;
            editor_piece = Instantiate(GM.white_king_template, new Vector3(26f, -4f, 0f), Quaternion.identity);
            editor_piece.GetComponent<template_equip>().color = 1;
            editor_piece.GetComponent<template_equip>().type = 6;

            editor_piece = Instantiate(GM.black_pawn_template, new Vector3(28f, 3.5f, 0f), Quaternion.identity);
            editor_piece.GetComponent<template_equip>().color = -1;
            editor_piece.GetComponent<template_equip>().type = 1;
            editor_piece = Instantiate(GM.black_bishop_template, new Vector3(28f, 2f, 0f), Quaternion.identity);
            editor_piece.GetComponent<template_equip>().color = -1;
            editor_piece.GetComponent<template_equip>().type = 3;
            editor_piece = Instantiate(GM.black_knight_template, new Vector3(28f, 0.5f, 0f), Quaternion.identity);
            editor_piece.GetComponent<template_equip>().color = -1;
            editor_piece.GetComponent<template_equip>().type = 2;
            editor_piece = Instantiate(GM.black_rook_template, new Vector3(28f, -1f, 0f), Quaternion.identity);
            editor_piece.GetComponent<template_equip>().color = -1;
            editor_piece.GetComponent<template_equip>().type = 4;
            editor_piece = Instantiate(GM.black_queen_template, new Vector3(28f, -2.5f, 0f), Quaternion.identity);
            editor_piece.GetComponent<template_equip>().color = -1;
            editor_piece.GetComponent<template_equip>().type = 5;
            editor_piece = Instantiate(GM.black_king_template, new Vector3(28f, -4f, 0f), Quaternion.identity);
            editor_piece.GetComponent<template_equip>().color = -1;
            editor_piece.GetComponent<template_equip>().type = 6;


            GameObject bin  = Instantiate(GM.bin, new Vector3(27f, 5f, 0f), Quaternion.identity);
            // blockers_masks.Add(new List<List<ulong>>());
            // blockers_masks.Add(new List<List<ulong>>());
            // for(int i=0; i<64; i++)
            // {
            //     blockers_masks[0].Add(new List<ulong>());
            //     blockers_masks[1].Add(new List<ulong>());
            // }
            init_bitboards();
        }


        //Tworzenie pozycji z FEN-u
        public void create_from_FEN(string FEN)
        {
            if (FEN == "")
            {
                return;
            }
            //index pilnuje cały czas na jakim znaku fena jesteśmy - przydaje się do flag pozycji
            int index = 0;

            clear_board();


            //Wczytanie pozycji bez flag
            Dictionary<char, piece_type> fen_dictionary = new Dictionary<char, piece_type>
            {
            { 'p', piece_type.pawn },
            { 'n', piece_type.knight },
            { 'b', piece_type.bishop },
            { 'r', piece_type.rook },
            { 'q', piece_type.queen },
            { 'k', piece_type.king }
            };

            int file = 0;
            int rank = 7;

            foreach (char c in FEN)
            {
                index++;
                if (c == ' ')
                {
                    break;
                }
                if (c == '/')
                {
                    file = 0;
                    rank--;
                }
                else
                {
                    if (char.IsDigit(c))
                    {
                        file += int.Parse(c.ToString());
                    }
                    else
                    {
                        int color = 1;
                        if (char.ToLower(c) == c)
                        {
                            color = -1;
                        }
                        if (!fen_dictionary.ContainsKey(char.ToLower(c)))
                        {
                            Debug.Log("Niepoprawny FEN");
                            clear_board();
                            return;
                        }
                        int type = (int)fen_dictionary[char.ToLower(c)];
                        add_piece(type, color, rank * 8 + file);
                        file++;
                    }
                }
            }

            //USTAWIENIE WSZYSTKICH FLAG

            //Ruch białych / czarnych
            if (FEN[index] == 'w')
            {
                white_move = true;
            }
            else
            {
                white_move = false;
            }
            index += 2;

            //Roszady
            char temp = FEN[index];
            is_white_castle_short_possible = false;
            is_white_castle_long_possible = false;
            is_black_castle_short_possible = false;
            is_black_castle_long_possible = false;

            while (Char.IsLetter(temp))
            {
                switch (temp)
                {
                    case 'K':
                        is_white_castle_short_possible = true;
                        break;
                    case 'Q':
                        is_white_castle_long_possible = true;
                        break;
                    case 'k':
                        is_black_castle_short_possible = true;
                        break;
                    case 'q':
                        is_black_castle_long_possible = true;
                        break;
                    default:
                        break;
                }
                index++;
                temp = FEN[index];
            }
            index++;


            //Pole enpassant
            if (FEN[index] == '-')
            {
                enpassant_square = -1;
            }
            else
            {
                Debug.Log(FEN[index] - 'a' + " " + (FEN[index + 1] - '1') * 8);
                enpassant_square = FEN[index] - 'a' + (FEN[++index] - '1') * 8;
            }
            index += 2;

            //Halfclock do remisu            
            string halfmove = "";
            char tempCharClock = FEN[index];
            while (Char.IsNumber(tempCharClock))
            {
                halfmove += tempCharClock;
                index++;
                tempCharClock = FEN[index];
            }
            fifty_move_counter = int.Parse(halfmove);
            index++;

            //Move count fenu
            string move_number = "";
            char temp_move_count = FEN[index];
            while (Char.IsNumber(temp_move_count))
            {
                move_number += temp_move_count;
                index++;
                if (index > FEN.Length - 1)
                {
                    break;
                }
                temp_move_count = FEN[index];
            }
            move_fen_counter = int.Parse(move_number);


            //Dodanie enpassant do listy żeby rozmiar się zgadzał => enpassant przy cofaniu ma zawsze rozmiar 2 => pierwszy enp[0] jest init tutaj , a drugi dodany w momencie ruchu dlatego !!!!!!!!! ZAWSZE czyta się koniec-2 a usuwa koniec-1 !!!!!!!!!!!!!!!!!!

            en_passant_history.Add(enpassant_square);

            manager.official_bitboard=all_pieces_bitboard;
            manager.main_string=all_pieces_bitboard.ToString();
            manager.temp=manager.main_string;

        }
        public void CreateVisualFromBoard()
        {
            //Robi pełną szachownicę ingame z boardu 
            //debug_print_board();
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (board[i * 8 + j].piece != null)
                    {
                        if (board[i * 8 + j].piece.type != 0)
                        {
                            if (board[i * 8 + j].piece.color == 1)
                            {
                                GameObject G = Instantiate(manager.white_templates[board[i * 8 + j].piece.type - 1], new Vector3(startPos.x + j * 1.28f, startPos.y + i * 1.28f, 0), quaternion.identity);
                                G.GetComponent<collideScript>().sq = i * 8 + j;
                                G.GetComponent<collideScript>().GM = manager;
                                ingame_pieces.Add(G);
                            }
                            else
                            {
                                GameObject G = Instantiate(manager.black_templates[board[i * 8 + j].piece.type - 1], new Vector3(startPos.x + j * 1.28f, startPos.y + i * 1.28f, 0), quaternion.identity);
                                G.GetComponent<collideScript>().sq = i * 8 + j;
                                G.GetComponent<collideScript>().GM = manager;
                                ingame_pieces.Add(G);
                            }
                        }
                    }
                }

            }
        }
        public void DestroyVisual()
        {
            //Usuwa wszystko co wyżej 
            for (int i = 0; i <= ingame_pieces.Count - 1; i++)
            {
                Destroy(ingame_pieces[i]);
            }
            ingame_pieces.Clear();
        }


        //Czyści wszystkie informacje o szachownicy: bb, listy, nulluje całą szachownicę
        public void clear_board()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    board[i * 8 + j].piece = null;
                }
            }

            three_move_history = new List<List<int>>();
            history_index = new List<Move>();
            en_passant_history = new List<int>();

            for (int i = 0; i <= white_pieces.Count - 1; i++)
            {
                for (int j = 0; j <= white_pieces[i].Count - 1; j++)
                {
                    white_pieces[i].Clear();
                }
            }
            for (int i = 0; i <= black_pieces.Count - 1; i++)
            {
                for (int j = 0; j <= black_pieces[i].Count - 1; j++)
                {
                    black_pieces[i].Clear();
                }
            }
            for (int i = 0; i <= all_pieces.Count - 1; i++)
            {
                for (int j = 0; j <= all_pieces[i].Count - 1; j++)
                {
                    all_pieces[i].Clear();
                }
            }


            white_pieces_bitboard = 0;
            black_pieces_bitboard = 0;
            all_pieces_bitboard = 0;
            for (int i = 0; i <= pieces_bitboards.Count - 1; i++)
            {
                pieces_bitboards[i] = 0;
            }
        }

        public void debug_print_board()
        {
            //Rzuca do konsoli mniej więcej sformatowaną szachownicę +/- = kolor {1,2...} => typ
            string text = "";
            for (int i = 7; i >= 0; i--)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (board[i * 8 + j].piece != null)
                    {
                        if (board[i * 8 + j].piece.color == 1)
                        {
                            text += " " + board[i * 8 + j].piece.type + " ";
                        }
                        else
                        {
                            text += -board[i * 8 + j].piece.type + " ";
                        }
                    }
                    else
                    {
                        text += " 0";
                    }
                }
                text += "\n";
            }
            Debug.Log(text);
        }

        //Dodaje pionek na określone miejsce do board i bb
        public Piece add_piece(int type, int color, int index)
        {
            Piece temp = new Piece(type, color, index);
            board[index].piece = temp;
            if (color == 1)
            {
                white_pieces[type - 1].Add(temp);
                white_pieces_bitboard |= 1UL << index;

                all_pieces[type - 1].Add(temp);
                pieces_bitboards[type - 1] |= 1UL << index;
                all_pieces_bitboard |= 1UL << index;
            }
            else
            {
                black_pieces[type - 1].Add(temp);
                black_pieces_bitboard |= 1UL << index;

                all_pieces[type + 5].Add(temp);
                pieces_bitboards[type + 5] |= 1UL << index;
                all_pieces_bitboard |= 1UL << index;
            }
            return temp;
        }

        //Dodaje pionek na określone miejsce do board i bb ale przyjmuje figure jako argument uzywany do cofania
        public void add_piece_from_graveyard(Piece oldPiece)
        {
            board[oldPiece.square].piece = oldPiece;

            if (oldPiece.color == 1)
            {
                white_pieces[oldPiece.type - 1].Add(oldPiece);
                white_pieces_bitboard |= 1UL << oldPiece.square;

                all_pieces[oldPiece.type - 1].Add(oldPiece);
                pieces_bitboards[oldPiece.type - 1] |= 1UL << oldPiece.square;
                all_pieces_bitboard |= 1UL << oldPiece.square;
            }
            else
            {
                black_pieces[oldPiece.type - 1].Add(oldPiece);
                black_pieces_bitboard |= 1UL << oldPiece.square;

                all_pieces[oldPiece.type + 5].Add(oldPiece);
                pieces_bitboards[oldPiece.type + 5] |= 1UL << oldPiece.square;
                all_pieces_bitboard |= 1UL << oldPiece.square;
            }
        }


        //Niszczy wszędzie dany pionek ;; ruch ma go zapisany
        public void destroy_piece(int sq)
        {
            if (board[sq].piece != null)
            {
                if (board[sq].piece.color == 1)
                {
                    white_pieces[board[sq].piece.type - 1].Remove(board[sq].piece);
                    all_pieces[board[sq].piece.type - 1].Remove(board[sq].piece);

                    white_pieces_bitboard &= ~(1UL << sq);
                    pieces_bitboards[board[sq].piece.type - 1] &= ~(1UL << sq);
                    all_pieces_bitboard &= ~(1UL << sq);

                    board[sq].piece = null;
                }
                else
                {
                    black_pieces[board[sq].piece.type - 1].Remove(board[sq].piece);
                    all_pieces[board[sq].piece.type + 5].Remove(board[sq].piece);

                    black_pieces_bitboard &= ~(1UL << sq);
                    pieces_bitboards[board[sq].piece.type + 5] &= ~(1UL << sq);
                    all_pieces_bitboard &= ~(1UL << sq);
                }

                board[sq].piece = null;
            }
        }

        //Robi ruch na szachownicy, ale nie aktualizuję grafik, żeby zaaktualizować musisz użyć funkcji create_visual
        public void make_move(Move move)
        {
            enpassant_square = -1;
            if (white_move && board[move.start_square].piece.color == -1)
                return;
            if (!white_move && board[move.start_square].piece.color == 1)
                return;

            int start_square = move.start_square;
            int target_square = move.target_square;

            if (start_square != target_square)
            {

                if (board[start_square].piece != null)
                {
                    if (move.piece_taken != null)
                        destroy_piece(move.piece_taken.square);


                    if (board[start_square].piece.color == 1)
                    {
                        pieces_bitboards[board[start_square].piece.type - 1] ^= (1UL << start_square) | (1UL << target_square);
                        all_pieces_bitboard ^= (1UL << start_square) | (1UL << target_square);
                        white_pieces_bitboard ^= (1UL << start_square) | (1UL << target_square);
                    }
                    else
                    {
                        pieces_bitboards[board[start_square].piece.type + 5] ^= (1UL << start_square) | (1UL << target_square);
                        all_pieces_bitboard ^= (1UL << start_square) | (1UL << target_square);
                        black_pieces_bitboard ^= (1UL << start_square) | (1UL << target_square);
                    }

                    if (board[start_square].piece.type == (int)piece_type.pawn && Math.Abs(move.start_square - move.target_square) == 16)
                    {
                        if (white_move)
                        {
                            if (isAttackedBy(start_square + 8, -1, 1))
                            {
                                enpassant_square = start_square + 8;
                            }
                        }
                        else
                        {
                            if (isAttackedBy(start_square - 8, 1, 1))
                            {
                                enpassant_square = start_square - 8;
                            }
                        }
                    }
                    en_passant_history.Add(enpassant_square);

                    board[target_square].piece = board[start_square].piece;
                    board[target_square].piece.square = move.target_square;
                    board[target_square].piece.moved++;
                    board[start_square].piece = null;

                    if (move.piece_promoted != null)
                    {
                        destroy_piece(target_square);
                        add_piece(move.piece_promoted.type, white_move ? 1 : -1, target_square);
                    }

                    if (move.is_white_castle_short)
                    {
                        pieces_bitboards[(int)piece_type.rook - 1] ^= (1UL << 7) | (1UL << 5);
                        all_pieces_bitboard ^= (1UL << 7) | (1UL << 5);
                        white_pieces_bitboard ^= (1UL << 7) | (1UL << 5);
                        board[5].piece = board[7].piece;
                        board[5].piece.square = 5;
                        board[5].piece.moved++;
                        board[7].piece = null;
                    }

                    if (move.is_white_castle_long)
                    {
                        pieces_bitboards[(int)piece_type.rook - 1] ^= (1UL << 3) | (1UL << 0);
                        all_pieces_bitboard ^= (1UL << 3) | (1UL << 0);
                        white_pieces_bitboard ^= (1UL << 3) | (1UL << 0);
                        board[3].piece = board[0].piece;
                        board[3].piece.square = 3;
                        board[3].piece.moved++;
                        board[0].piece = null;
                    }

                    if (move.is_black_castle_short)
                    {
                        pieces_bitboards[(int)piece_type.rook + 5] ^= (1UL << 63) | (1UL << 61);
                        all_pieces_bitboard ^= (1UL << 63) | (1UL << 61);
                        black_pieces_bitboard ^= (1UL << 63) | (1UL << 61);
                        board[61].piece = board[63].piece;
                        board[61].piece.square = 61;
                        board[61].piece.moved++;
                        board[63].piece = null;
                    }

                    if (move.is_black_castle_long)
                    {
                        pieces_bitboards[(int)piece_type.rook + 5] ^= (1UL << 56) | (1UL << 59);
                        all_pieces_bitboard ^= (1UL << 56) | (1UL << 59);
                        black_pieces_bitboard ^= (1UL << 56) | (1UL << 59);
                        board[59].piece = board[56].piece;
                        board[59].piece.square = 59;
                        board[59].piece.moved++;
                        board[56].piece = null;
                    }


                    white_move = !white_move;
                    history_index.Add(move);
                }
                if (board[target_square].piece.color == -1)
                {
                    move_fen_counter++;
                }
                if (move.piece_taken != null || board[target_square].piece.type == 1)
                {
                    fifty_move_counter = 0;
                }
                else
                {
                    fifty_move_counter++;
                }
            }
        }


        //Cofa ruch też trzeba zaaktualizować wygląd
        public void undo_move(Move move)
        {
            if (history_index.Count <= 1)
                return;
            if (white_move)
            {
                Task.Run(async () =>
                {
                    Debug.Log("Cofnięcie ruchu na lichess");
                    string response = await manager.ApiHandleTakebackP2(manager.challangeID, true);
                    Debug.Log(response);
                    response = await manager.ApiHandleTakebackP1(manager.challangeID, true);
                    Debug.Log(response);
                });
            }
            else
            {
                Task.Run(async () =>
                {
                    Debug.Log("Cofnięcie ruchu na lichess");
                    string response = await manager.ApiHandleTakebackP1(manager.challangeID, true);
                    Debug.Log(response);
                    response = await manager.ApiHandleTakebackP2(manager.challangeID, true);
                    Debug.Log(response);
                });

            }

            if (three_move_history.Count > 0)
                three_move_history.Remove(three_move_history.Last());

            if (move.piece_promoted != null)
            {
                destroy_piece(move.target_square);
                add_piece_from_graveyard(move.pawn_used_for_promotion);
            }
            board[move.start_square].piece = board[move.target_square].piece;
            board[move.start_square].piece.square = move.start_square;
            board[move.start_square].piece.moved--;
            board[move.target_square].piece = null;

            if (en_passant_history.Count >= 2)
            {
                enpassant_square = en_passant_history[en_passant_history.Count - 2];
            }
            if (en_passant_history.Count == 1)
            {
                enpassant_square = en_passant_history[0];
            }
            if (en_passant_history.Count == 0)
            {
                enpassant_square = -1;
            }

            en_passant_history.RemoveAt(en_passant_history.Count - 1);






            if (board[move.start_square].piece.color == 1)
            {
                pieces_bitboards[board[move.start_square].piece.type - 1] ^= (1UL << move.start_square) | (1UL << move.target_square);
                all_pieces_bitboard ^= (1UL << move.start_square) | (1UL << move.target_square);
                white_pieces_bitboard ^= (1UL << move.start_square) | (1UL << move.target_square);

                if (move.is_white_castle_short)
                {
                    board[7].piece = board[5].piece;
                    board[7].piece.square = 7;
                    board[7].piece.moved--;
                    board[5].piece = null;
                    pieces_bitboards[(int)piece_type.rook - 1] ^= (1UL << 5) | (1UL << 7);
                    all_pieces_bitboard ^= (1UL << 5) | (1UL << 7);
                    white_pieces_bitboard ^= (1UL << 5) | (1UL << 7);
                }

                if (move.is_white_castle_long)
                {
                    board[0].piece = board[3].piece;
                    board[0].piece.square = 0;
                    board[0].piece.moved--;
                    board[3].piece = null;
                    pieces_bitboards[(int)piece_type.rook - 1] ^= (1UL << 0) | (1UL << 3);
                    all_pieces_bitboard ^= (1UL << 0) | (1UL << 3);
                    white_pieces_bitboard ^= (1UL << 0) | (1UL << 3);
                }
            }
            else
            {
                pieces_bitboards[board[move.start_square].piece.type + 5] ^= (1UL << move.start_square) | (1UL << move.target_square);
                all_pieces_bitboard ^= (1UL << move.start_square) | (1UL << move.target_square);
                black_pieces_bitboard ^= (1UL << move.start_square) | (1UL << move.target_square);

                if (move.is_black_castle_short)
                {
                    board[63].piece = board[61].piece;
                    board[63].piece.square = 63;
                    board[63].piece.moved--;
                    board[61].piece = null;
                    pieces_bitboards[(int)piece_type.rook + 5] ^= (1UL << 61) | (1UL << 63);
                    all_pieces_bitboard ^= (1UL << 61) | (1UL << 63);
                    black_pieces_bitboard ^= (1UL << 61) | (1UL << 63);
                }

                if (move.is_black_castle_long)
                {
                    board[56].piece = board[59].piece;
                    board[56].piece.square = 56;
                    board[56].piece.moved--;
                    board[59].piece = null;
                    pieces_bitboards[(int)piece_type.rook + 5] ^= (1UL << 56) | (1UL << 59);
                    all_pieces_bitboard ^= (1UL << 56) | (1UL << 59);
                    black_pieces_bitboard ^= (1UL << 56) | (1UL << 59);
                }
            }

            if (move.piece_taken != null)
                add_piece_from_graveyard(move.piece_taken);

            white_move = !white_move;
            history_index.RemoveAt(history_index.Count - 1);
            fifty_move_counter--;
            fifty_move_counter = Math.Max(fifty_move_counter, 0);
            if (board[move.start_square].piece.color == -1)
            {
                move_fen_counter--;
            }
        }

        void init_bitboards()
        {
            generate_bishop_bitboard_attacks();
            generate_knight_bitboard_attacks();
            generate_rook_bitboard_attacks();
            generate_rook_bitboard_mask();
            generate_bishop_bitboard_mask();
            generate_blockers_mask_rooks();
            generate_blockers_mask_bishops();

            //dotąd wszystko raczej działa
            generate_pawn_moves();
            generate_pawn_moves_black();
            generate_king_moves();
            setup_magic_attacks_bishop();
            setup_magic_attacks_rooks();
            generate_rectangular_lookup();

            // for(int i=0; i<=63; i++)
            // {
            //     bishop_magics.Add(FindMagicBishop(bishop_bitboard_mask[i],i));
            //     Debug.Log("Pole "+i+" Magic: "+bishop_magics[i]);
            //     rook_magics.Add(FindMagicRook(rook_bitboard_mask[i],i));
            //     Debug.Log("Pole "+i+" Magic: "+rook_magics[i]);
            // }
            // string rooks="";
            // foreach(ulong number in rook_magics)
            // {
            //     rooks+=number;
            //     rooks+=", ";
            // }
            // Debug.Log(rooks);
            // string bishops="";
            // foreach(ulong number in bishop_magics)
            // {
            //     bishops+=number;
            //     bishops+=", ";
            // }
            // Debug.Log(bishops);
        }


        public bool is_white_castling_short_pos(int checks)
        {
            if (board[5].piece == null && board[6].piece == null && board[7].piece != null && white_pieces[(int)piece_type.king - 1][0].moved <= 0 && board[7].piece.moved <= 0 && board[7].piece.type == (int)piece_type.rook && board[7].piece.color == 1 && !isAttackedBy(5, -1) && !isAttackedBy(6, -1) && checks <= 0 && is_white_castle_short_possible)
            {
                return true;
            }
            return false;
        }

        public bool is_white_castling_long_pos(int checks)
        {
            if (board[1].piece == null && board[2].piece == null && board[3].piece == null && board[0].piece != null && white_pieces[(int)piece_type.king - 1][0].moved <= 0 && board[0].piece.moved <= 0 && board[0].piece.type == (int)piece_type.rook && board[0].piece.color == 1 && !isAttackedBy(2, -1) && !isAttackedBy(3, -1) && checks <= 0 && is_white_castle_long_possible)
            {
                return true;
            }
            return false;
        }

        public bool is_black_castling_short_pos(int checks)
        {
            if (board[61].piece == null && board[62].piece == null && board[63].piece != null && black_pieces[(int)piece_type.king - 1][0].moved <= 0 && board[63].piece.moved <= 0 && board[63].piece.type == (int)piece_type.rook && board[63].piece.color == -1 && !isAttackedBy(61, 1) && !isAttackedBy(62, 1) && checks <= 0 && is_black_castle_short_possible)
            {
                return true;
            }
            return false;
        }

        public bool is_black_castling_long_pos(int checks)
        {
            if (board[57].piece == null && board[58].piece == null && board[59].piece == null && board[56].piece != null && black_pieces[(int)piece_type.king - 1][0].moved <= 0 && board[56].piece.moved <= 0 && board[56].piece.type == (int)piece_type.rook && board[56].piece.color == -1 && !isAttackedBy(58, 1) && !isAttackedBy(59, 1) && checks <= 0 && is_black_castle_long_possible)
            {
                return true;
            }
            return false;
        }
        //Generacja ruchów - trzon programu
        public List<Move> MoveGen()
        {
            check_map = ulong.MaxValue;
            ResetPins();
            pinned_pieces = GeneratePinned();
            List<Move> movesList = new List<Move>();
            if (fifty_move_counter >= 100)
            {
                return movesList;
            }
            if (white_move)
            {
                int checks = HowManyChecks(white_pieces[(int)piece_type.king - 1][0].square, -1);
                if (checks < 2)
                {
                    foreach (Piece pawn in white_pieces[0])
                    {
                        if (board[pawn.square + 8].piece == null && ((1UL << (pawn.square + 8)) & pawn.pin_mask & check_map) != 0)
                        {
                            if ((pawn.square + 8) / 8 == 7)
                            {
                                Move temp_knight = new Move(pawn.square, pawn.square + 8);
                                temp_knight.pawn_used_for_promotion = pawn;
                                temp_knight.piece_promoted = new Piece((int)piece_type.knight, 1, pawn.square + 8);
                                movesList.Add(temp_knight);
                                Move temp_bishop = new Move(pawn.square, pawn.square + 8);
                                temp_bishop.pawn_used_for_promotion = pawn;
                                temp_bishop.piece_promoted = new Piece((int)piece_type.bishop, 1, pawn.square + 8);
                                movesList.Add(temp_bishop);
                                Move temp_rook = new Move(pawn.square, pawn.square + 8);
                                temp_rook.pawn_used_for_promotion = pawn;
                                temp_rook.piece_promoted = new Piece((int)piece_type.rook, 1, pawn.square + 8);
                                movesList.Add(temp_rook);
                                Move temp_queen = new Move(pawn.square, pawn.square + 8);
                                temp_queen.pawn_used_for_promotion = pawn;
                                temp_queen.piece_promoted = new Piece((int)piece_type.queen, 1, pawn.square + 8);
                                movesList.Add(temp_queen);
                            }
                            else
                            {
                                movesList.Add(new Move(pawn.square, pawn.square + 8));
                            }
                        }
                        if (board[pawn.square].piece.moved == 0 && pawn.square < 16)
                        {
                            if (board[pawn.square + 16].piece == null && (1UL << pawn.square + 16 & pawn.pin_mask & check_map) != 0 && board[pawn.square + 8].piece == null)
                            {
                                movesList.Add(new Move(pawn.square, pawn.square + 16));
                            }
                        }
                        ulong possCaptures = pawn_attacks[0][pawn.square] & black_pieces_bitboard & pawn.pin_mask & check_map;

                        for (int i = 0; i < 64; i++)
                        {
                            if (((possCaptures >> i) & 1) == 1 && ((possCaptures & pawn.pin_mask & check_map) != 0))
                            {
                                if ((pawn.square + 8) / 8 == 7)
                                {
                                    Move temp_knight = new Move(pawn.square, i);
                                    temp_knight.pawn_used_for_promotion = pawn;
                                    temp_knight.piece_taken = board[i].piece;
                                    temp_knight.piece_promoted = new Piece((int)piece_type.knight, 1, i);
                                    movesList.Add(temp_knight);
                                    Move temp_bishop = new Move(pawn.square, i);
                                    temp_bishop.pawn_used_for_promotion = pawn;
                                    temp_bishop.piece_taken = board[i].piece;
                                    temp_bishop.piece_promoted = new Piece((int)piece_type.bishop, 1, i);
                                    movesList.Add(temp_bishop);
                                    Move temp_rook = new Move(pawn.square, i);
                                    temp_rook.pawn_used_for_promotion = pawn;
                                    temp_rook.piece_taken = board[i].piece;
                                    temp_rook.piece_promoted = new Piece((int)piece_type.rook, 1, i);
                                    movesList.Add(temp_rook);
                                    Move temp_queen = new Move(pawn.square, i);
                                    temp_queen.pawn_used_for_promotion = pawn;
                                    temp_queen.piece_taken = board[i].piece;
                                    temp_queen.piece_promoted = new Piece((int)piece_type.queen, 1, i);
                                    movesList.Add(temp_queen);
                                }
                                else
                                {
                                    Move tempMove = new Move(pawn.square, i);
                                    tempMove.piece_taken = board[i].piece;
                                    movesList.Add(tempMove);
                                }

                            }
                        }

                        if (enpassant_square > 0)
                        {
                            if ((pawn_attacks[0][pawn.square] & (1UL << enpassant_square) & pawn.pin_mask & check_map) == (1UL << enpassant_square) || ((pawn_attacks[0][pawn.square] & (1UL << (enpassant_square)) & pawn.pin_mask) == (1UL << enpassant_square) && ((check_map & (1UL << enpassant_square - 8)) != 0)))
                            {
                                Move tempMove = new Move(pawn.square, enpassant_square);
                                tempMove.piece_taken = board[enpassant_square - 8].piece;
                                movesList.Add(tempMove);
                            }
                        }
                    }
                    foreach (Piece knight in white_pieces[(int)piece_type.knight - 1])
                    {
                        ulong tempMoves = bitboard_attacks_wo_mask[1][knight.square] & ~white_pieces_bitboard & knight.pin_mask & check_map;
                        for (int i = 0; i < 64; i++)
                        {
                            if (((tempMoves >> i) & 1) == 1)
                            {
                                Move move = new Move(knight.square, i);
                                if (board[i].piece != null)
                                {
                                    move.piece_taken = board[i].piece;
                                }
                                movesList.Add(move);
                            }
                        }
                    }

                    foreach (Piece rook in white_pieces[(int)piece_type.rook - 1])
                    {
                        ulong tempMoves = rook_moves[rook.square][TransformMagicIndex(all_pieces_bitboard & rook_bitboard_mask[rook.square], rook_magics[rook.square], rook_bits[rook.square])] & rook.pin_mask & check_map;
                        tempMoves &= ~white_pieces_bitboard;
                        for (int i = 0; i < 64; i++)
                        {
                            if (((tempMoves >> i) & 1) == 1)
                            {
                                Move move = new Move(rook.square, i);
                                if (board[i].piece != null)
                                {
                                    move.piece_taken = board[i].piece;
                                }
                                movesList.Add(move);
                            }
                        }
                    }

                    foreach (Piece bishop in white_pieces[(int)piece_type.bishop - 1])
                    {
                        ulong tempMoves = bishop_moves[bishop.square][TransformMagicIndex(all_pieces_bitboard & bishop_bitboard_mask[bishop.square], bishop_magics[bishop.square], bishop_bits[bishop.square])] & bishop.pin_mask & check_map;
                        tempMoves &= ~white_pieces_bitboard;
                        for (int i = 0; i < 64; i++)
                        {
                            if (((tempMoves >> i) & 1) == 1)
                            {
                                Move move = new Move(bishop.square, i);
                                if (board[i].piece != null)
                                {
                                    move.piece_taken = board[i].piece;
                                }
                                movesList.Add(move);
                            }
                        }
                    }
                    foreach (Piece queen in white_pieces[(int)piece_type.queen - 1])
                    {
                        ulong tempMoves = bishop_moves[queen.square][TransformMagicIndex(all_pieces_bitboard & bishop_bitboard_mask[queen.square], bishop_magics[queen.square], bishop_bits[queen.square])] & queen.pin_mask & check_map;
                        tempMoves |= rook_moves[queen.square][TransformMagicIndex(all_pieces_bitboard & rook_bitboard_mask[queen.square], rook_magics[queen.square], rook_bits[queen.square])] & queen.pin_mask & check_map;
                        tempMoves &= ~white_pieces_bitboard;
                        for (int i = 0; i < 64; i++)
                        {
                            if (((tempMoves >> i) & 1) == 1)
                            {
                                Move move = new Move(queen.square, i);
                                if (board[i].piece != null)
                                {
                                    move.piece_taken = board[i].piece;
                                }
                                movesList.Add(move);
                            }
                        }
                    }
                }


                foreach (Piece king in white_pieces[(int)piece_type.king - 1])
                {
                    ulong tempMoves = king_moves[king.square];
                    for (int i = 0; i < 64; i++)
                    {
                        if (((tempMoves >> i) & 1) == 1)
                        {
                            if (isAttackedBy(i, -1, true))
                            {
                                tempMoves &= ~(1UL << i);
                            }
                        }
                    }
                    tempMoves &= ~white_pieces_bitboard;
                    for (int i = 0; i < 64; i++)
                    {
                        if (((tempMoves >> i) & 1) == 1)
                        {
                            Move move = new Move(king.square, i);
                            if (board[i].piece != null)
                            {
                                move.piece_taken = board[i].piece;
                            }
                            movesList.Add(move);
                        }
                    }
                    if (is_white_castling_short_pos(checks))
                    {
                        Move move = new Move(king.square, 6);
                        move.is_white_castle_short = true;
                        movesList.Add(move);
                    }
                    if (is_white_castling_long_pos(checks))
                    {
                        Move move = new Move(king.square, 2);
                        move.is_white_castle_long = true;
                        movesList.Add(move);
                    }
                }
                // if (movesList.Count <= 0)
                // {
                //     if (checks >= 1)
                //     {
                //         Debug.Log("mat");
                //     }
                //     else
                //     {
                //         Debug.Log("pat");
                //     }
                // }
            }
            else
            {
                int checks = HowManyChecks(black_pieces[(int)piece_type.king - 1][0].square, 1);

                if (checks < 2)
                {
                    foreach (Piece pawn in black_pieces[0])
                    {
                        if (board[pawn.square - 8].piece == null && ((1UL << (pawn.square - 8)) & pawn.pin_mask & check_map) != 0)
                        {
                            if ((pawn.square - 8) / 8 == 0)
                            {
                                Move temp_knight = new Move(pawn.square, pawn.square - 8);
                                temp_knight.pawn_used_for_promotion = pawn;
                                temp_knight.piece_promoted = new Piece((int)piece_type.knight, -1, pawn.square - 8);
                                movesList.Add(temp_knight);
                                Move temp_bishop = new Move(pawn.square, pawn.square - 8);
                                temp_bishop.pawn_used_for_promotion = pawn;
                                temp_bishop.piece_promoted = new Piece((int)piece_type.bishop, -1, pawn.square - 8);
                                movesList.Add(temp_bishop);
                                Move temp_rook = new Move(pawn.square, pawn.square - 8);
                                temp_rook.pawn_used_for_promotion = pawn;
                                temp_rook.piece_promoted = new Piece((int)piece_type.rook, -1, pawn.square - 8);
                                movesList.Add(temp_rook);
                                Move temp_queen = new Move(pawn.square, pawn.square - 8);
                                temp_queen.pawn_used_for_promotion = pawn;
                                temp_queen.piece_promoted = new Piece((int)piece_type.queen, -1, pawn.square - 8);
                                movesList.Add(temp_queen);
                            }
                            else
                            {
                                movesList.Add(new Move(pawn.square, pawn.square - 8));
                            }
                        }
                        if (board[pawn.square].piece.moved == 0 && pawn.square > 47)
                        {
                            if (board[pawn.square - 16].piece == null && ((1UL << (pawn.square - 16)) & pawn.pin_mask & check_map) != 0 && board[pawn.square - 8].piece == null)
                            {
                                movesList.Add(new Move(pawn.square, pawn.square - 16));
                            }
                        }
                        ulong possCaptures = pawn_attacks[1][pawn.square] & white_pieces_bitboard & pawn.pin_mask & check_map;

                        for (int i = 0; i < 63; i++)
                        {
                            if (((possCaptures >> i) & 1) == 1 && ((possCaptures & pawn.pin_mask & check_map) != 0))
                            {
                                if ((pawn.square - 8) / 8 == 0)
                                {
                                    Move temp_knight = new Move(pawn.square, i);
                                    temp_knight.pawn_used_for_promotion = pawn;
                                    temp_knight.piece_taken = board[i].piece;
                                    temp_knight.piece_promoted = new Piece((int)piece_type.knight, -1, i);
                                    movesList.Add(temp_knight);
                                    Move temp_bishop = new Move(pawn.square, i);
                                    temp_bishop.pawn_used_for_promotion = pawn;
                                    temp_bishop.piece_taken = board[i].piece;
                                    temp_bishop.piece_promoted = new Piece((int)piece_type.bishop, -1, i);
                                    movesList.Add(temp_bishop);
                                    Move temp_rook = new Move(pawn.square, i);
                                    temp_rook.pawn_used_for_promotion = pawn;
                                    temp_rook.piece_taken = board[i].piece;
                                    temp_rook.piece_promoted = new Piece((int)piece_type.rook, -1, i);
                                    movesList.Add(temp_rook);
                                    Move temp_queen = new Move(pawn.square, i);
                                    temp_queen.pawn_used_for_promotion = pawn;
                                    temp_queen.piece_taken = board[i].piece;
                                    temp_queen.piece_promoted = new Piece((int)piece_type.queen, -1, i);
                                    movesList.Add(temp_queen);
                                }
                                else
                                {
                                    Move tempMove = new Move(pawn.square, i);
                                    tempMove.piece_taken = board[i].piece;
                                    movesList.Add(tempMove);
                                }
                            }
                        }

                        if (enpassant_square > 0 && (pawn_attacks[1][pawn.square] & (1UL << enpassant_square) & pawn.pin_mask & check_map) == (1UL << enpassant_square) || ((pawn_attacks[1][pawn.square] & (1UL << (enpassant_square)) & pawn.pin_mask) == (1UL << enpassant_square) && ((check_map & (1UL << enpassant_square + 8)) != 0)))
                        {
                            Move tempMove = new Move(pawn.square, enpassant_square);
                            tempMove.piece_taken = board[enpassant_square + 8].piece;
                            movesList.Add(tempMove);
                        }
                    }
                    foreach (Piece knight in black_pieces[(int)piece_type.knight - 1])
                    {
                        ulong tempMoves = bitboard_attacks_wo_mask[1][knight.square] & ~black_pieces_bitboard & knight.pin_mask & check_map;

                        for (int i = 0; i < 64; i++)
                        {
                            if (((tempMoves >> i) & 1) == 1)
                            {
                                Move move = new Move(knight.square, i);

                                if (board[i].piece != null)
                                {
                                    move.piece_taken = board[i].piece;
                                }

                                movesList.Add(move);
                            }
                        }
                    }

                    foreach (Piece rook in black_pieces[(int)piece_type.rook - 1])
                    {
                        ulong tempMoves = rook_moves[rook.square][TransformMagicIndex(all_pieces_bitboard & rook_bitboard_mask[rook.square], rook_magics[rook.square], rook_bits[rook.square])] & rook.pin_mask & check_map;
                        tempMoves &= ~black_pieces_bitboard;
                        for (int i = 0; i < 64; i++)
                        {
                            if (((tempMoves >> i) & 1) == 1)
                            {
                                Move move = new Move(rook.square, i);

                                if (board[i].piece != null)
                                {
                                    move.piece_taken = board[i].piece;
                                }

                                movesList.Add(move);
                            }
                        }
                    }

                    foreach (Piece bishop in black_pieces[(int)piece_type.bishop - 1])
                    {
                        ulong tempMoves = bishop_moves[bishop.square][TransformMagicIndex(all_pieces_bitboard & bishop_bitboard_mask[bishop.square], bishop_magics[bishop.square], bishop_bits[bishop.square])] & bishop.pin_mask & check_map;
                        tempMoves &= ~black_pieces_bitboard;

                        for (int i = 0; i < 64; i++)
                        {
                            if (((tempMoves >> i) & 1) == 1)
                            {
                                Move move = new Move(bishop.square, i);

                                if (board[i].piece != null)
                                {
                                    move.piece_taken = board[i].piece;
                                }

                                movesList.Add(move);
                            }
                        }
                    }
                    foreach (Piece queen in black_pieces[(int)piece_type.queen - 1])
                    {
                        ulong tempMoves = bishop_moves[queen.square][TransformMagicIndex(all_pieces_bitboard & bishop_bitboard_mask[queen.square], bishop_magics[queen.square], bishop_bits[queen.square])] & queen.pin_mask & check_map;
                        tempMoves |= rook_moves[queen.square][TransformMagicIndex(all_pieces_bitboard & rook_bitboard_mask[queen.square], rook_magics[queen.square], rook_bits[queen.square])] & queen.pin_mask & check_map;
                        tempMoves &= ~black_pieces_bitboard;
                        for (int i = 0; i < 64; i++)
                        {
                            if (((tempMoves >> i) & 1) == 1)
                            {
                                Move move = new Move(queen.square, i);

                                if (board[i].piece != null)
                                {
                                    move.piece_taken = board[i].piece;
                                }

                                movesList.Add(move);
                            }
                        }
                    }
                }



                foreach (Piece king in black_pieces[(int)piece_type.king - 1])
                {
                    ulong tempMoves = king_moves[king.square];

                    for (int i = 0; i < 64; i++)
                    {
                        if (((tempMoves >> i) & 1) == 1)
                        {
                            if (isAttackedBy(i, 1, false))
                            {
                                tempMoves &= ~(1UL << i);
                            }
                        }
                    }

                    tempMoves &= ~black_pieces_bitboard;

                    for (int i = 0; i < 64; i++)
                    {
                        if (((tempMoves >> i) & 1) == 1)
                        {
                            Move move = new Move(king.square, i);

                            if (board[i].piece != null)
                            {
                                move.piece_taken = board[i].piece;
                            }

                            movesList.Add(move);
                        }
                    }

                    if (is_black_castling_short_pos(checks))
                    {
                        Move move = new Move(king.square, 62);
                        move.is_black_castle_short = true;
                        movesList.Add(move);
                    }
                    if (is_black_castling_long_pos(checks))
                    {
                        Move move = new Move(king.square, 58);
                        move.is_black_castle_long = true;
                        movesList.Add(move);
                    }
                }

                // if (movesList.Count <= 0)
                // {
                //     if (checks >= 1)
                //     {
                //         Debug.Log("mat");
                //     }
                //     else
                //     {
                //         Debug.Log("pat");
                //     }
                // }

            }
            return movesList;
        }

        //Sprawdzanie czy dane pole jest atakowane np do tego gdzie król może się ruszyć 
        bool isAttackedBy(int sq, int color)
        {
            if (color == 1)
            {
                foreach (Piece pawn in white_pieces[(int)piece_type.pawn - 1])
                {
                    if ((pawn_attacks[0][pawn.square] & (1UL << sq)) != 0)
                    {
                        return true;
                    }
                }

                foreach (Piece knight in white_pieces[(int)piece_type.knight - 1])
                {
                    if ((bitboard_attacks_wo_mask[1][knight.square] & (1UL << sq)) != 0)
                    {
                        return true;
                    }
                }

                foreach (Piece rook in white_pieces[(int)piece_type.rook - 1])
                {
                    if ((rook_moves[rook.square][TransformMagicIndex(all_pieces_bitboard & rook_bitboard_mask[rook.square], rook_magics[rook.square], rook_bits[rook.square])] & (1UL << sq)) != 0)
                    {
                        return true;
                    }
                }

                foreach (Piece bishop in white_pieces[(int)piece_type.bishop - 1])
                {
                    if ((bishop_moves[bishop.square][TransformMagicIndex(all_pieces_bitboard & bishop_bitboard_mask[bishop.square], bishop_magics[bishop.square], bishop_bits[bishop.square])] & (1UL << sq)) != 0)
                    {
                        return true;
                    }
                }

                foreach (Piece queen in white_pieces[(int)piece_type.queen - 1])
                {
                    ulong tempMoves = rook_moves[queen.square][TransformMagicIndex(all_pieces_bitboard & rook_bitboard_mask[queen.square], rook_magics[queen.square], rook_bits[queen.square])] | bishop_moves[queen.square][TransformMagicIndex(all_pieces_bitboard & bishop_bitboard_mask[queen.square], bishop_magics[queen.square], bishop_bits[queen.square])];

                    if ((tempMoves & (1UL << sq)) != 0)
                    {
                        return true;
                    }
                }

                foreach (Piece king in white_pieces[(int)piece_type.king - 1])
                {
                    if ((king_moves[sq] & (1UL << sq)) != 0)
                    {
                        return true;
                    }
                }
            }
            else
            {
                foreach (Piece pawn in black_pieces[(int)piece_type.pawn - 1])
                {
                    if ((pawn_attacks[1][pawn.square] & (1UL << sq)) != 0)
                    {
                        return true;
                    }
                }

                foreach (Piece knight in black_pieces[(int)piece_type.knight - 1])
                {
                    if ((bitboard_attacks_wo_mask[1][knight.square] & (1UL << sq)) != 0)
                    {
                        return true;
                    }
                }

                foreach (Piece rook in black_pieces[(int)piece_type.rook - 1])
                {
                    if ((rook_moves[rook.square][TransformMagicIndex(all_pieces_bitboard & rook_bitboard_mask[rook.square], rook_magics[rook.square], rook_bits[rook.square])] & (1UL << sq)) != 0)
                    {
                        return true;
                    }
                }

                foreach (Piece bishop in black_pieces[(int)piece_type.bishop - 1])
                {
                    if ((bishop_moves[bishop.square][TransformMagicIndex(all_pieces_bitboard & bishop_bitboard_mask[bishop.square], bishop_magics[bishop.square], bishop_bits[bishop.square])] & (1UL << sq)) != 0)
                    {
                        return true;
                    }
                }

                foreach (Piece queen in black_pieces[(int)piece_type.queen - 1])
                {
                    ulong tempMoves = rook_moves[queen.square][TransformMagicIndex(all_pieces_bitboard & rook_bitboard_mask[queen.square], rook_magics[queen.square], rook_bits[queen.square])] | bishop_moves[queen.square][TransformMagicIndex(all_pieces_bitboard & bishop_bitboard_mask[queen.square], bishop_magics[queen.square], bishop_bits[queen.square])];

                    if ((tempMoves & (1UL << sq)) != 0)
                    {
                        return true;
                    }
                }

                foreach (Piece king in black_pieces[(int)piece_type.king - 1])
                {
                    if ((king_moves[sq] & (1UL << sq)) != 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        bool isAttackedBy(int sq, int color, bool king_back)
        {
            if (color == 1)
            {
                foreach (Piece pawn in white_pieces[(int)piece_type.pawn - 1])
                {
                    if ((pawn_attacks[0][pawn.square] & (1UL << sq)) != 0)
                    {
                        return true;
                    }
                }

                foreach (Piece knight in white_pieces[(int)piece_type.knight - 1])
                {
                    if ((bitboard_attacks_wo_mask[1][knight.square] & (1UL << sq)) != 0)
                    {
                        return true;
                    }
                }

                foreach (Piece rook in white_pieces[(int)piece_type.rook - 1])
                {
                    if ((rook_moves[rook.square][TransformMagicIndex(all_pieces_bitboard & rook_bitboard_mask[rook.square] & ~(1UL << black_pieces[(int)piece_type.king - 1][0].square), rook_magics[rook.square], rook_bits[rook.square])] & (1UL << sq)) != 0)
                    {
                        return true;
                    }
                }

                foreach (Piece bishop in white_pieces[(int)piece_type.bishop - 1])
                {
                    if ((bishop_moves[bishop.square][TransformMagicIndex(all_pieces_bitboard & bishop_bitboard_mask[bishop.square] & ~(1UL << black_pieces[(int)piece_type.king - 1][0].square), bishop_magics[bishop.square], bishop_bits[bishop.square])] & (1UL << sq)) != 0)
                    {
                        return true;
                    }
                }

                foreach (Piece queen in white_pieces[(int)piece_type.queen - 1])
                {
                    ulong tempMoves = rook_moves[queen.square][TransformMagicIndex(all_pieces_bitboard & rook_bitboard_mask[queen.square] & ~(1UL << black_pieces[(int)piece_type.king - 1][0].square), rook_magics[queen.square], rook_bits[queen.square])] | bishop_moves[queen.square][TransformMagicIndex(all_pieces_bitboard & bishop_bitboard_mask[queen.square] & ~(1UL << black_pieces[(int)piece_type.king - 1][0].square), bishop_magics[queen.square], bishop_bits[queen.square])];

                    if ((tempMoves & (1UL << sq)) != 0)
                    {
                        return true;
                    }
                }

                foreach (Piece king in white_pieces[(int)piece_type.king - 1])
                {
                    if ((king_moves[king.square] & (1UL << sq)) != 0)
                    {
                        return true;
                    }
                }
            }
            else
            {
                foreach (Piece pawn in black_pieces[(int)piece_type.pawn - 1])
                {
                    if ((pawn_attacks[1][pawn.square] & (1UL << sq)) != 0)
                    {
                        return true;
                    }
                }

                foreach (Piece knight in black_pieces[(int)piece_type.knight - 1])
                {
                    if ((bitboard_attacks_wo_mask[1][knight.square] & (1UL << sq)) != 0)
                    {
                        return true;
                    }
                }

                foreach (Piece rook in black_pieces[(int)piece_type.rook - 1])
                {
                    if ((rook_moves[rook.square][TransformMagicIndex(all_pieces_bitboard & rook_bitboard_mask[rook.square] & ~(1UL << white_pieces[(int)piece_type.king - 1][0].square), rook_magics[rook.square], rook_bits[rook.square])] & (1UL << sq)) != 0)
                    {
                        return true;
                    }
                }

                foreach (Piece bishop in black_pieces[(int)piece_type.bishop - 1])
                {
                    if ((bishop_moves[bishop.square][TransformMagicIndex(all_pieces_bitboard & bishop_bitboard_mask[bishop.square] & ~(1UL << white_pieces[(int)piece_type.king - 1][0].square), bishop_magics[bishop.square], bishop_bits[bishop.square])] & (1UL << sq)) != 0)
                    {
                        return true;
                    }
                }

                foreach (Piece queen in black_pieces[(int)piece_type.queen - 1])
                {
                    ulong tempMoves = rook_moves[queen.square][TransformMagicIndex(all_pieces_bitboard & rook_bitboard_mask[queen.square] & ~(1UL << white_pieces[(int)piece_type.king - 1][0].square), rook_magics[queen.square], rook_bits[queen.square])] | bishop_moves[queen.square][TransformMagicIndex(all_pieces_bitboard & bishop_bitboard_mask[queen.square] & ~(1UL << white_pieces[(int)piece_type.king - 1][0].square), bishop_magics[queen.square], bishop_bits[queen.square])];

                    if ((tempMoves & (1UL << sq)) != 0)
                    {
                        return true;
                    }
                }

                foreach (Piece king in black_pieces[(int)piece_type.king - 1])
                {
                    if ((king_moves[king.square] & (1UL << sq)) != 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }



        bool isAttackedBy(int sq, int color, int pawns)
        {
            if (color == 1)
            {
                foreach (Piece pawn in white_pieces[(int)piece_type.pawn - 1])
                {
                    if ((pawn_attacks[0][pawn.square] & (1UL << sq)) != 0)
                    {
                        return true;
                    }
                }
            }
            else
            {
                foreach (Piece pawn in black_pieces[(int)piece_type.pawn - 1])
                {
                    if ((pawn_attacks[1][pawn.square] & (1UL << sq)) != 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        //Przy double checku można się ruszać tylko królem
        public int HowManyChecks(int sq, int color)
        {
            int attackers = 0;
            check_map = 0;

            if (color == 1)
            {
                foreach (Piece pawn in white_pieces[(int)piece_type.pawn - 1])
                {
                    if ((pawn_attacks[0][pawn.square] & (1UL << sq)) != 0)
                    {
                        attackers += 1;
                        check_map |= (1UL << pawn.square) | rectangular_lookup[pawn.square][sq];
                    }
                }

                foreach (Piece knight in white_pieces[(int)piece_type.knight - 1])
                {
                    if ((bitboard_attacks_wo_mask[1][knight.square] & (1UL << sq)) != 0)
                    {
                        attackers += 1;
                        check_map |= (1UL << knight.square) | rectangular_lookup[knight.square][sq];
                    }
                }

                foreach (Piece rook in white_pieces[(int)piece_type.rook - 1])
                {
                    if ((rook_moves[rook.square][TransformMagicIndex(all_pieces_bitboard & rook_bitboard_mask[rook.square], rook_magics[rook.square], rook_bits[rook.square])] & (1UL << sq)) != 0)
                    {
                        attackers += 1;
                        check_map |= (1UL << rook.square) | rectangular_lookup[rook.square][sq];
                    }
                }

                foreach (Piece bishop in white_pieces[(int)piece_type.bishop - 1])
                {
                    if ((bishop_moves[bishop.square][TransformMagicIndex(all_pieces_bitboard & bishop_bitboard_mask[bishop.square], bishop_magics[bishop.square], bishop_bits[bishop.square])] & (1UL << sq)) != 0)
                    {
                        attackers += 1;
                        check_map |= (1UL << bishop.square) | rectangular_lookup[bishop.square][sq];
                    }
                }
                foreach (Piece queen in white_pieces[(int)piece_type.queen - 1])
                {
                    if ((rook_moves[queen.square][TransformMagicIndex(all_pieces_bitboard & rook_bitboard_mask[queen.square], rook_magics[queen.square], rook_bits[queen.square])] & (1UL << sq)) != 0)
                    {
                        attackers += 1;
                        check_map |= (1UL << queen.square) | rectangular_lookup[queen.square][sq];
                    }

                    if ((bishop_moves[queen.square][TransformMagicIndex(all_pieces_bitboard & bishop_bitboard_mask[queen.square], bishop_magics[queen.square], bishop_bits[queen.square])] & (1UL << sq)) != 0)
                    {
                        attackers += 1;
                        check_map |= (1UL << queen.square) | rectangular_lookup[queen.square][sq];
                    }
                }

                foreach (Piece king in white_pieces[(int)piece_type.king - 1])
                {
                    if ((king_moves[sq] & (1UL << sq)) != 0)
                    {
                        attackers += 1;
                        check_map |= (1UL << king.square) | rectangular_lookup[king.square][sq];
                    }
                }
            }
            else
            {
                foreach (Piece pawn in black_pieces[(int)piece_type.pawn - 1])
                {
                    if ((pawn_attacks[1][pawn.square] & (1UL << sq)) != 0)
                    {
                        attackers += 1;
                        check_map |= (1UL << pawn.square) | rectangular_lookup[pawn.square][sq];
                    }
                }

                foreach (Piece knight in black_pieces[(int)piece_type.knight - 1])
                {
                    if ((bitboard_attacks_wo_mask[1][knight.square] & (1UL << sq)) != 0)
                    {
                        attackers += 1;
                        check_map |= (1UL << knight.square) | rectangular_lookup[knight.square][sq];
                    }
                }
                foreach (Piece rook in black_pieces[(int)piece_type.rook - 1])
                {
                    if ((rook_moves[rook.square][TransformMagicIndex(all_pieces_bitboard & rook_bitboard_mask[rook.square], rook_magics[rook.square], rook_bits[rook.square])] & (1UL << sq)) != 0)
                    {
                        attackers += 1;
                        check_map |= (1UL << rook.square) | rectangular_lookup[rook.square][sq];
                    }
                }

                foreach (Piece bishop in black_pieces[(int)piece_type.bishop - 1])
                {
                    if ((bishop_moves[bishop.square][TransformMagicIndex(all_pieces_bitboard & bishop_bitboard_mask[bishop.square], bishop_magics[bishop.square], bishop_bits[bishop.square])] & (1UL << sq)) != 0)
                    {
                        attackers += 1;
                        check_map |= (1UL << bishop.square) | rectangular_lookup[bishop.square][sq];
                    }
                }

                foreach (Piece queen in black_pieces[(int)piece_type.queen - 1])
                {
                    if ((rook_moves[queen.square][TransformMagicIndex(all_pieces_bitboard & rook_bitboard_mask[queen.square], rook_magics[queen.square], rook_bits[queen.square])] & (1UL << sq)) != 0)
                    {
                        attackers += 1;
                        check_map |= (1UL << queen.square) | rectangular_lookup[queen.square][sq];
                    }

                    if ((bishop_moves[queen.square][TransformMagicIndex(all_pieces_bitboard & bishop_bitboard_mask[queen.square], bishop_magics[queen.square], bishop_bits[queen.square])] & (1UL << sq)) != 0)
                    {
                        attackers += 1;
                        check_map |= (1UL << queen.square) | rectangular_lookup[queen.square][sq];
                    }
                }

                foreach (Piece king in black_pieces[(int)piece_type.king - 1])
                {
                    if ((king_moves[sq] & (1UL << sq)) != 0)
                    {
                        attackers += 1;
                        check_map |= (1UL << king.square) | rectangular_lookup[king.square][sq];
                    }
                }
            }

            if (check_map == 0)
                check_map = ulong.MaxValue;


            return attackers;
        }

        //Sprawdzanie co jest związane
        //Funkcja wpisuje do każdej figury możliwość ruchu i czy jest związana  
        public ulong GeneratePinned()
        {
            ulong pinned = 0;

            if (white_move)
            {
                ulong pinners_rook = XRAYRooks(white_pieces_bitboard, white_pieces[(int)piece_type.king - 1][0].square) & (pieces_bitboards[(int)piece_type.rook + 5] | pieces_bitboards[(int)piece_type.queen + 5]);
                ulong pinners_bishop = XRAYBishops(white_pieces_bitboard, white_pieces[(int)piece_type.king - 1][0].square) & (pieces_bitboards[(int)piece_type.bishop + 5] | pieces_bitboards[(int)piece_type.queen + 5]);

                for (int i = 0; i < 64; i++)
                {
                    if ((pinners_rook & (1UL << i)) != 0)
                    {
                        pinned |= rectangular_lookup[i][white_pieces[(int)piece_type.king - 1][0].square] & white_pieces_bitboard;
                        board[BbToIndex(rectangular_lookup[i][white_pieces[(int)piece_type.king - 1][0].square] & white_pieces_bitboard)].piece.pin_mask = rectangular_lookup[i][white_pieces[(int)piece_type.king - 1][0].square] | (1UL << i);
                    }
                }

                for (int i = 0; i < 64; i++)
                {
                    if ((pinners_bishop & (1UL << i)) != 0)
                    {
                        pinned |= rectangular_lookup[i][white_pieces[(int)piece_type.king - 1][0].square] & white_pieces_bitboard;
                        board[BbToIndex(rectangular_lookup[i][white_pieces[(int)piece_type.king - 1][0].square] & white_pieces_bitboard)].piece.pin_mask = rectangular_lookup[i][white_pieces[(int)piece_type.king - 1][0].square] | (1UL << i);
                    }
                }

                if (enpassant_square > 0)
                {
                    if ((enpassant_square - 8) / 8 == white_pieces[(int)piece_type.king - 1][0].square / 8)
                    {
                        foreach (var pawn in white_pieces[(int)piece_type.pawn - 1])
                        {
                            if ((pawn_attacks[0][pawn.square] & (1UL << enpassant_square)) != 0)
                            {
                                ulong mask_wo_pawns = all_pieces_bitboard & ~(1UL << (enpassant_square - 8)) & ~(1UL << pawn.square);
                                ulong temp = rook_moves[white_pieces[(int)piece_type.king - 1][0].square][TransformMagicIndex(mask_wo_pawns & rook_bitboard_mask[white_pieces[(int)piece_type.king - 1][0].square], rook_magics[white_pieces[(int)piece_type.king - 1][0].square], rook_bits[white_pieces[(int)piece_type.king - 1][0].square])] & (255UL << (white_pieces[(int)piece_type.king - 1][0].square / 8 * 8));

                                if ((temp & (pieces_bitboards[(int)piece_type.rook + 5] | pieces_bitboards[(int)piece_type.queen + 5])) != 0)
                                {
                                    pawn.pin_mask &= ~(1UL << enpassant_square);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                ulong pinners_rook = XRAYRooks(black_pieces_bitboard, black_pieces[(int)piece_type.king - 1][0].square) & (pieces_bitboards[(int)piece_type.rook - 1] | pieces_bitboards[(int)piece_type.queen - 1]);
                ulong pinners_bishop = XRAYBishops(black_pieces_bitboard, black_pieces[(int)piece_type.king - 1][0].square) & (pieces_bitboards[(int)piece_type.bishop - 1] | pieces_bitboards[(int)piece_type.queen - 1]);

                for (int i = 0; i < 64; i++)
                {
                    if ((pinners_rook & (1UL << i)) != 0)
                    {
                        pinned |= rectangular_lookup[i][black_pieces[(int)piece_type.king - 1][0].square] & black_pieces_bitboard;
                        board[BbToIndex(rectangular_lookup[i][black_pieces[(int)piece_type.king - 1][0].square] & black_pieces_bitboard)].piece.pin_mask = rectangular_lookup[i][black_pieces[(int)piece_type.king - 1][0].square] | (1UL << i);
                    }
                }

                for (int i = 0; i < 64; i++)
                {
                    if ((pinners_bishop & (1UL << i)) != 0)
                    {
                        pinned |= rectangular_lookup[i][black_pieces[(int)piece_type.king - 1][0].square] & black_pieces_bitboard;
                        board[BbToIndex(rectangular_lookup[i][black_pieces[(int)piece_type.king - 1][0].square] & black_pieces_bitboard)].piece.pin_mask = rectangular_lookup[i][black_pieces[(int)piece_type.king - 1][0].square] | (1UL << i);
                    }
                }

                if (enpassant_square > 0)
                {
                    if ((enpassant_square + 8) / 8 == black_pieces[(int)piece_type.king - 1][0].square / 8)
                    {
                        foreach (var pawn in black_pieces[(int)piece_type.pawn - 1])
                        {
                            if ((pawn_attacks[1][pawn.square] & (1UL << enpassant_square)) != 0)
                            {
                                ulong mask_wo_pawns = all_pieces_bitboard & ~(1UL << (enpassant_square + 8)) & ~(1UL << pawn.square);
                                ulong temp = rook_moves[black_pieces[(int)piece_type.king - 1][0].square][TransformMagicIndex(mask_wo_pawns & rook_bitboard_mask[black_pieces[(int)piece_type.king - 1][0].square], rook_magics[black_pieces[(int)piece_type.king - 1][0].square], rook_bits[black_pieces[(int)piece_type.king - 1][0].square])] & (255UL << (black_pieces[(int)piece_type.king - 1][0].square / 8 * 8));

                                if ((temp & (pieces_bitboards[(int)piece_type.rook - 1] | pieces_bitboards[(int)piece_type.queen - 1])) != 0)
                                {
                                    pawn.pin_mask &= ~(1UL << enpassant_square);
                                }
                            }
                        }
                    }
                }
            }

            return pinned;
        }

        //Zwraca pola za sojusznikami
        ulong XRAYRooks(ulong blockers, int from)
        {
            ulong at = rook_moves[from][TransformMagicIndex(all_pieces_bitboard & rook_bitboard_mask[from], rook_magics[from], rook_bits[from])];
            ulong collide_blockers = at & blockers;
            return at ^ rook_moves[from][TransformMagicIndex((all_pieces_bitboard ^ collide_blockers) & rook_bitboard_mask[from], rook_magics[from], rook_bits[from])];
        }

        ulong XRAYBishops(ulong blockers, int from)
        {
            ulong at = bishop_moves[from][TransformMagicIndex(all_pieces_bitboard & bishop_bitboard_mask[from], bishop_magics[from], bishop_bits[from])];
            ulong collide_blockers = at & blockers;
            return at ^ bishop_moves[from][TransformMagicIndex((all_pieces_bitboard ^ collide_blockers) & bishop_bitboard_mask[from], bishop_magics[from], bishop_bits[from])];
        }

        //Zmienia bb na index przykładowo 8 mapuje się do 2^3 czyli index to będzie 3
        int BbToIndex(ulong bb)
        {
            for (int i = 0; i < 64; i++)
            {
                if ((bb & (1UL << i)) != 0)
                {
                    return i;
                }
            }
            return -1; // Zwraca -1, jeśli nie znaleziono żadnej wartości
        }

        //Czyści piny po ruchu
        void ResetPins()
        {
            for (int i = 0; i < 64; i++)
            {
                if (board[i].piece != null)
                {
                    board[i].piece.pin_mask = ulong.MaxValue; // Ustawia maskę przypięcia na maksymalną wartość ulong
                }
            }
        }



        //patrz rectangular lookup
        public void generate_rectangular_lookup()
        {
            for (int i = 0; i < 64; i++)
            {
                rectangular_lookup.Add(new List<ulong>());
                for (int j = 0; j < 64; j++)
                {
                    rectangular_lookup[i].Add(0);
                }
            }

            for (int i = 0; i < 64; i++)
            {
                int row_from = i / 8;
                int col_from = i % 8;

                ulong temp_bb = 0;

                for (int j = col_from + 2; j < 8; j++)
                {
                    int temp = 8 * row_from + j - 1;
                    temp_bb |= 1UL << temp;
                    rectangular_lookup[i][temp + 1] = temp_bb;
                }

                temp_bb = 0;

                for (int j = col_from - 2; j >= 0; j--)
                {
                    int temp = 8 * row_from + j + 1;
                    temp_bb |= 1UL << temp;
                    rectangular_lookup[i][temp - 1] = temp_bb;
                }

                temp_bb = 0;

                for (int j = row_from + 2; j < 8; j++)
                {
                    int temp = 8 * j + col_from - 8;
                    temp_bb |= 1UL << temp;
                    rectangular_lookup[i][temp + 8] = temp_bb;
                }

                temp_bb = 0;

                for (int j = row_from - 2; j >= 0; j--)
                {
                    int temp = 8 * j + col_from + 8;
                    temp_bb |= 1UL << temp;
                    rectangular_lookup[i][temp - 8] = temp_bb;
                }

                int row = i / 8;
                int col = i % 8;
                int r = row + 2;
                int c = col - 2;
                temp_bb = 0;

                while (r < 8 && c >= 0)
                {
                    temp_bb |= 1UL << (r * 8 + c - 7);
                    rectangular_lookup[i][r * 8 + c] = temp_bb;
                    r++;
                    c--;
                }

                r = row + 2;
                c = col + 2;
                temp_bb = 0;

                while (r < 8 && c < 8)
                {
                    temp_bb |= 1UL << (r * 8 + c - 9);
                    rectangular_lookup[i][r * 8 + c] = temp_bb;
                    r++;
                    c++;
                }

                r = row - 2;
                c = col - 2;
                temp_bb = 0;

                while (r >= 0 && c >= 0)
                {
                    temp_bb |= 1UL << (r * 8 + c + 9);
                    rectangular_lookup[i][r * 8 + c] = temp_bb;
                    r--;
                    c--;
                }

                r = row - 2;
                c = col + 2;
                temp_bb = 0;

                while (r >= 0 && c < 8)
                {
                    temp_bb |= 1UL << (r * 8 + c + 7);
                    rectangular_lookup[i][r * 8 + c] = temp_bb;
                    r--;
                    c++;
                }
            }
        }
        // OD TEGO MIEJSCA W DÓŁ JEST GŁÓWNIE GENRACJA RUCHÓW POMIJAM PISANIE DOKUMENTACJI BO TEN KOD WYKONUJE SIĘ
        // TYLKO I WYŁĄCZNIE RAZ PRZY ROZPOCZĘCIU PROGRAMU I W GŁÓWNEJ MIERZE DZIEJĘ SIĘ TO SAMO
        // OD TEGO MOMENTU W DÓŁ NIE MA DOKUMENTACJI

        //generuje poza maske jest git
        void generate_rook_bitboard_attacks()
        {
            List<ulong> rook_moves = new List<ulong>();

            for (int i = 0; i < 64; i++)
            {
                ulong moves = 0;
                int row = i / 8;
                int col = i % 8;

                for (int j = col + 1; j < 8; j++)
                {
                    int temp = 8 * row + j;
                    moves |= 1UL << temp;
                }

                for (int j = col - 1; j >= 0; j--)
                {
                    int temp = 8 * row + j;
                    moves |= 1UL << temp;
                }

                for (int j = row + 1; j < 8; j++)
                {
                    int temp = 8 * j + col;
                    moves |= 1UL << temp;
                }

                for (int j = row - 1; j >= 0; j--)
                {
                    int temp = 8 * j + col;
                    moves |= 1UL << temp;
                }

                rook_moves.Add(moves);
                //Debug.Log(moves);
            }

            bitboard_attacks_wo_mask.Add(rook_moves);
        }


        //maski działają
        void generate_rook_bitboard_mask()
        {
            for (int i = 0; i < 64; i++)
            {
                ulong moves = 0;
                int row = i / 8;
                int col = i % 8;

                for (int j = col + 1; j < 7; j++)
                {
                    int temp = 8 * row + j;
                    moves |= 1UL << temp;
                }

                for (int j = col - 1; j > 0; j--)
                {
                    int temp = 8 * row + j;
                    moves |= 1UL << temp;
                }

                for (int j = row + 1; j < 7; j++)
                {
                    int temp = 8 * j + col;
                    moves |= 1UL << temp;
                }

                for (int j = row - 1; j > 0; j--)
                {
                    int temp = 8 * j + col;
                    moves |= 1UL << temp;
                }

                rook_bitboard_mask.Add(moves);
            }
        }


        ulong GenerateRookBitboardAttacksBlockers(int sq, ulong blockers)
        {
            ulong moves = 0;
            int row = sq / 8;
            int col = sq % 8;

            for (int j = col + 1; j < 8; j++)
            {
                int temp = 8 * row + j;
                moves |= 1UL << temp;
                if ((blockers & (1UL << temp)) == (1UL << temp))
                    break;
            }

            for (int j = col - 1; j >= 0; j--)
            {
                int temp = 8 * row + j;
                moves |= 1UL << temp;
                if ((blockers & (1UL << temp)) == (1UL << temp))
                    break;
            }

            for (int j = row + 1; j < 8; j++)
            {
                int temp = 8 * j + col;
                moves |= 1UL << temp;
                if ((blockers & (1UL << temp)) == (1UL << temp))
                    break;
            }

            for (int j = row - 1; j >= 0; j--)
            {
                int temp = 8 * j + col;
                moves |= 1UL << temp;
                if ((blockers & (1UL << temp)) == (1UL << temp))
                    break;
            }

            return moves;
        }


        //Generuje pełny atak poza maske działa dobrze !!!
        void generate_bishop_bitboard_attacks()
        {
            List<ulong> bishop_moves = new List<ulong>();

            for (int i = 0; i < 64; i++)
            {
                ulong moves = 0;
                int row = i / 8;
                int col = i % 8;

                int r = row + 1;
                int c = col - 1;
                while (r < 8 && c >= 0)
                {
                    moves |= 1UL << (r * 8 + c);
                    r++;
                    c--;
                }

                r = row + 1;
                c = col + 1;
                while (r < 8 && c < 8)
                {
                    moves |= 1UL << (r * 8 + c);
                    r++;
                    c++;
                }

                r = row - 1;
                c = col - 1;
                while (r >= 0 && c >= 0)
                {
                    moves |= 1UL << (r * 8 + c);
                    r--;
                    c--;
                }

                r = row - 1;
                c = col + 1;
                while (r >= 0 && c < 8)
                {
                    moves |= 1UL << (r * 8 + c);
                    r--;
                    c++;
                }
                bishop_moves.Add(moves);
                //Debug.Log(moves);
            }

            bitboard_attacks_wo_mask.Add(bishop_moves);
        }

        ulong GenerateBishopBitboardAttacksBlockers(int sq, ulong blockers)
        {
            ulong moves = 0;
            int row = sq / 8;
            int col = sq % 8;

            int r = row + 1;
            int c = col - 1;
            while (r < 8 && c >= 0)
            {
                moves |= 1UL << (r * 8 + c);
                if ((blockers & (1UL << (r * 8 + c))) == (1UL << (r * 8 + c)))
                    break;

                r++;
                c--;
            }

            r = row + 1;
            c = col + 1;
            while (r < 8 && c < 8)
            {
                moves |= 1UL << (r * 8 + c);
                if ((blockers & (1UL << (r * 8 + c))) == (1UL << (r * 8 + c)))
                    break;

                r++;
                c++;
            }

            r = row - 1;
            c = col - 1;
            while (r >= 0 && c >= 0)
            {
                moves |= 1UL << (r * 8 + c);
                if ((blockers & (1UL << (r * 8 + c))) == (1UL << (r * 8 + c)))
                    break;

                r--;
                c--;
            }

            r = row - 1;
            c = col + 1;
            while (r >= 0 && c < 8)
            {
                moves |= 1UL << (r * 8 + c);
                if ((blockers & (1UL << (r * 8 + c))) == (1UL << (r * 8 + c)))
                    break;

                r--;
                c++;
            }

            return moves;
        }


        //maski działają
        void generate_bishop_bitboard_mask()
        {
            for (int i = 0; i < 64; i++)
            {
                ulong moves = 0;
                int row = i / 8;
                int col = i % 8;

                int r = row + 1;
                int c = col - 1;
                while (r < 7 && c >= 1)
                {
                    moves |= 1UL << (r * 8 + c);
                    r++;
                    c--;
                }

                r = row + 1;
                c = col + 1;
                while (r < 7 && c < 7)
                {
                    moves |= 1UL << (r * 8 + c);
                    r++;
                    c++;
                }

                r = row - 1;
                c = col - 1;
                while (r >= 1 && c >= 1)
                {
                    moves |= 1UL << (r * 8 + c);
                    r--;
                    c--;
                }

                r = row - 1;
                c = col + 1;
                while (r >= 1 && c < 7)
                {
                    moves |= 1UL << (r * 8 + c);
                    r--;
                    c++;
                }

                bishop_bitboard_mask.Add(moves);
            }
        }

        void generate_knight_bitboard_attacks()
        {
            List<ulong> knightMoves = new List<ulong>();

            for (int i = 0; i < 64; i++)
            {
                ulong moves = 0;
                int row = i / 8;
                int col = i % 8;

                Vector2[] offsets = new Vector2[]
                {
            new Vector2(-1, -2), new Vector2(-2, -1), new Vector2(-2, 1), new Vector2(-1, 2),
            new Vector2(1, -2), new Vector2(2, -1), new Vector2(2, 1), new Vector2(1, 2)
                };

                foreach (var offset in offsets)
                {
                    int newRow = row + (int)offset.x;
                    int newCol = col + (int)offset.y;

                    if (newRow >= 0 && newRow < 8 && newCol >= 0 && newCol < 8)
                        moves |= 1UL << (newRow * 8 + newCol);
                }

                knightMoves.Add(moves);
                //Debug.Log(moves);
            }

            bitboard_attacks_wo_mask.Add(knightMoves);
        }

        void generate_pawn_moves()
        {
            List<ulong> pawnMoves = new List<ulong>();
            List<ulong> captureMoves = new List<ulong>();

            for (int i = 0; i < 64; i++)
            {
                ulong moves = 0;
                ulong captures = 0;
                int row = i / 8;
                int col = i % 8;

                if (row < 7)
                    moves |= 1UL << ((row + 1) * 8 + col);

                if (row == 1)
                    moves |= 1UL << ((row + 2) * 8 + col);

                pawnMoves.Add(moves);

                if (col < 7)
                    captures |= 1UL << ((row + 1) * 8 + col + 1);

                if (col > 0)
                    captures |= 1UL << ((row + 1) * 8 + col - 1);

                captureMoves.Add(captures);
            }

            pawn_attacks.Add(captureMoves);
            pawn_moves.Add(pawnMoves);
        }

        void generate_pawn_moves_black()
        {
            List<ulong> pawnMoves = new List<ulong>();
            List<ulong> captureMoves = new List<ulong>();

            for (int i = 0; i < 64; i++)
            {
                ulong moves = 0;
                ulong captures = 0;
                int row = i / 8;
                int col = i % 8;

                if (row > 0)
                    moves |= 1UL << ((row - 1) * 8 + col);

                if (row == 6)
                    moves |= 1UL << ((row - 2) * 8 + col);

                pawnMoves.Add(moves);

                if (col < 7)
                    captures |= 1UL << ((row - 1) * 8 + col + 1);

                if (col > 0)
                    captures |= 1UL << ((row - 1) * 8 + col - 1);

                captureMoves.Add(captures);
            }

            pawn_attacks.Add(captureMoves);
            pawn_moves.Add(pawnMoves);
        }

        public void generate_king_moves()
        {
            for (int i = 0; i < 64; i++)
            {
                ulong moves = 0;
                int row = i / 8;
                int col = i % 8;

                if (row > 0)
                    moves |= 1UL << ((row - 1) * 8 + col);

                // Ruchy w dół
                if (row < 7)
                    moves |= 1UL << ((row + 1) * 8 + col);

                // Ruchy w lewo
                if (col > 0)
                    moves |= 1UL << (row * 8 + col - 1);

                // Ruchy w prawo
                if (col < 7)
                    moves |= 1UL << (row * 8 + col + 1);

                // Ruchy na skos w górę i w lewo
                if (row > 0 && col > 0)
                    moves |= 1UL << ((row - 1) * 8 + col - 1);

                // Ruchy na skos w górę i w prawo
                if (row > 0 && col < 7)
                    moves |= 1UL << ((row - 1) * 8 + col + 1);

                // Ruchy na skos w dół i w lewo
                if (row < 7 && col > 0)
                    moves |= 1UL << ((row + 1) * 8 + col - 1);

                // Ruchy na skos w dół i w prawo
                if (row < 7 && col < 7)
                    moves |= 1UL << ((row + 1) * 8 + col + 1);

                king_moves.Add(moves);
            }
        }


        //działa dobrze
        void generate_blockers_mask_rooks()
        {
            List<List<ulong>> blockers = new List<List<ulong>>();

            for (int i = 0; i <= 63; i++)
            {
                List<int> indxList = new List<int>();
                List<ulong> tempBlockers = new List<ulong>();

                for (int z = 0; z < 4096; z++)
                {
                    tempBlockers.Add(0);
                }

                for (int j = 0; j < 64; j++)
                {
                    if ((rook_bitboard_mask[i] & (1UL << j)) == 1UL << j)
                    {
                        indxList.Add(j);
                    }
                }

                for (int k = 0; k < Math.Pow(2, indxList.Count); k++)
                {
                    for (int l = 0; l < indxList.Count; l++)
                    {
                        //to przesunięcie to jest czekanie na bit - najmniejszy bit będzie na indexie startowym bo zostanie przesunięty o l czyli w może nie za pierwszym może nie za drugim ale za razem swojego indexu wpadnie na miejsce 0 a teraz można sprawdzić czy ma 0/1 a na koniec przesunąć go o wartość indexu z listy indexów, pamiętając że to działa bo 
                        //jeżeli przesunięcie o l nie wsunęło go na start to pomija się ten index bo wartość 0 przesunięta nic nie zmienia dzk temu że indx idzie z pętlą zawsze przesuwamy wg zasady cofamy o L (czyli trafia na start albo igonrujemy) jeśli git to przesuwamy ale nie o L, a o index[zaindeksowany lką] bo on przechowuje jakim indexem powinna być l
                        ulong bitVal = ((ulong)k >> l) & 1;
                        tempBlockers[k] |= bitVal << indxList[l];
                    }
                }

                blockers.Add(tempBlockers);
            }
            blockers_masks.Add(blockers);
        }

        //bloker maski działają
        public void generate_blockers_mask_bishops()
        {
            List<List<ulong>> blockers = new List<List<ulong>>();

            for (int i = 0; i <= 63; i++)
            {
                List<int> indxList = new List<int>();
                List<ulong> tempBlockers = new List<ulong>();

                for (int z = 0; z < 4096; z++)
                {
                    tempBlockers.Add(0);
                }

                for (int j = 0; j < 64; j++)
                {
                    if ((bishop_bitboard_mask[i] & (1UL << j)) == 1UL << j)
                    {
                        indxList.Add(j);
                    }
                }

                for (int k = 0; k < Math.Pow(2, indxList.Count); k++)
                {
                    for (int l = 0; l < indxList.Count; l++)
                    {
                        //u wieży jest to opisane
                        ulong bitVal = ((ulong)k >> l) & 1;
                        tempBlockers[k] |= bitVal << indxList[l];
                    }
                }

                blockers.Add(tempBlockers);

            }
            blockers_masks.Add(blockers);
        }


        public ulong RandomUInt64()
        {
            ulong u1 = (ulong)UnityEngine.Random.Range(0, 65536);
            ulong u2 = (ulong)UnityEngine.Random.Range(0, 65536);
            ulong u3 = (ulong)UnityEngine.Random.Range(0, 65536);
            ulong u4 = (ulong)UnityEngine.Random.Range(0, 65536);

            return u1 | (u2 << 16) | (u3 << 32) | (u4 << 48);
        }

        public ulong randomUintFewBits()
        {
            return RandomUInt64() & RandomUInt64() & RandomUInt64();
        }

        int Popcount(ulong bb)
        {
            int count = 0;

            for (int i = 0; i < 64; i++)
            {
                if ((bb & (1UL << i)) == 1UL << i)
                {
                    count++;
                }
            }

            return count;
        }

        ulong FindMagicBishop(ulong mask, int sq)
        {
            ulong validationCheck = 255UL << 56;
            int maxIndex;
            int lowIndex;

            for (int i = 0; i < 100000000; i++)
            {
                ulong magic = randomUintFewBits();
                bool skip = false;

                if (Popcount((mask * magic) & validationCheck) < 6)
                {
                    continue;
                }

                ulong[] used = new ulong[4096];
                maxIndex = 0;
                lowIndex = 4096;

                for (int j = 0; j < (1 << Popcount(mask)); j++)
                {
                    int tempIndex = TransformMagicIndex(blockers_masks[1][sq][j], magic, bishop_bits[sq]);
                    ulong at = GenerateBishopBitboardAttacksBlockers(sq, blockers_masks[1][sq][j]);

                    if (tempIndex > maxIndex)
                    {
                        maxIndex = tempIndex;
                    }

                    if (tempIndex < lowIndex)
                    {
                        lowIndex = tempIndex;
                    }

                    if (used[tempIndex] == 0)
                    {
                        used[tempIndex] = at;
                    }
                    else
                    {
                        if (used[tempIndex] != at)
                        {
                            skip = true;
                            break;
                        }
                    }
                }

                if (skip)
                {
                    continue;
                }

                Debug.Log(magic);
                Debug.Log(lowIndex);
                Debug.Log(maxIndex);
                return magic;
            }

            return 0; // or some other default value indicating failure
        }
        ulong FindMagicRook(ulong mask, int sq)
        {
            ulong validationCheck = 255UL << 56;
            int maxIndex;
            int lowIndex;

            for (int i = 0; i < 100000000; i++)
            {
                ulong magic = randomUintFewBits();
                bool skip = false;

                if (Popcount((mask * magic) & validationCheck) < 6)
                {
                    continue;
                }

                ulong[] used = new ulong[4096];
                maxIndex = 0;
                lowIndex = 4096;

                for (int j = 0; j < (1 << Popcount(mask)); j++)
                {
                    int tempIndex = TransformMagicIndex(blockers_masks[0][sq][j], magic, rook_bits[sq]);
                    ulong at = GenerateRookBitboardAttacksBlockers(sq, blockers_masks[0][sq][j]);

                    if (tempIndex > maxIndex)
                    {
                        maxIndex = tempIndex;
                    }

                    if (tempIndex < lowIndex)
                    {
                        lowIndex = tempIndex;
                    }

                    if (used[tempIndex] == 0)
                    {
                        used[tempIndex] = at;
                    }
                    else
                    {
                        if (used[tempIndex] != at)
                        {
                            skip = true;
                            break;
                        }
                    }
                }

                if (skip)
                {
                    continue;
                }

                Debug.Log(magic);
                Debug.Log(lowIndex);
                Debug.Log(maxIndex);
                return magic;
            }

            return 0; // or some other default value indicating failure
        }

        void setup_magic_attacks_bishop()
        {
            for (int i = 0; i < 64; i++)
            {
                ulong magic = bishop_magics[i];
                for (int j = 0; j < (1 << Popcount(bishop_bitboard_mask[i])); j++)
                {
                    int tempIndex = TransformMagicIndex(blockers_masks[1][i][j], magic, bishop_bits[i]);
                    ulong at = GenerateBishopBitboardAttacksBlockers(i, blockers_masks[1][i][j]);
                    // Debug.Log(blockers_masks[1][i][j] + " " + at + " pole: "+i);
                    bishop_moves[i][tempIndex] = at;
                }
            }
        }

        void setup_magic_attacks_rooks()
        {
            for (int i = 0; i < 64; i++)
            {
                ulong magic = rook_magics[i];

                for (int j = 0; j < (1 << Popcount(rook_bitboard_mask[i])); j++)
                {
                    int tempIndex = TransformMagicIndex(blockers_masks[0][i][j], magic, rook_bits[i]);
                    ulong at = GenerateRookBitboardAttacksBlockers(i, blockers_masks[0][i][j]);
                    rook_moves[i][tempIndex] = at;
                }
            }
        }

        public int TransformMagicIndex(ulong blockers, ulong magic, int bits)
        {
            return (int)((blockers * magic) >> (64 - bits));
        }


        List<int> rook_bits = new List<int>(){ 12, 11, 11, 11, 11, 11, 11, 12,
                                11, 10, 10, 10, 10, 10, 10, 11,
                                11, 10, 10, 10, 10, 10, 10, 11,
                                11, 10, 10, 10, 10, 10, 10, 11,
                                11, 10, 10, 10, 10, 10, 10, 11,
                                11, 10, 10, 10, 10, 10, 10, 11,
                                11, 10, 10, 10, 10, 10, 10, 11,
                                12, 11, 11, 11, 11, 11, 11, 12
        };


        List<int> bishop_bits = new List<int>(){
                                6, 5, 5, 5, 5, 5, 5, 6,
                                5, 5, 5, 5, 5, 5, 5, 5,
                                5, 5, 7, 7, 7, 7, 5, 5,
                                5, 5, 7, 9, 9, 7, 5, 5,
                                5, 5, 7, 9, 9, 7, 5, 5,
                                5, 5, 7, 7, 7, 7, 5, 5,
                                5, 5, 5, 5, 5, 5, 5, 5,
                                6, 5, 5, 5, 5, 5, 5, 6
        };
        public int counter = 0;
        public ulong Perft(int depth, int fulldepth)
        {
            if (depth == 0)
            {
                counter++;
                return 1UL;
            }


            ulong nodes = 0;
            ulong tempNodes = 0;
            List<Move> moves = MoveGen();
            if (moves.Count == 0)
            {
                return 0;
            }
            foreach (Move move in moves)
            {
                ulong bpieces = black_pieces_bitboard;
                make_move(move);
                tempNodes += Perft(depth - 1, depth);
                nodes += tempNodes;
                undo_move(move);
                // if(depth > 1 || (history_index[history_index.Count-1].start_square==61 && history_index[history_index.Count-1].target_square==34) && (history_index[history_index.Count-2].start_square==23 && history_index[history_index.Count-2].target_square==38))
                // {
                //     if(board[11].piece != null)
                //     {
                //         Debug.Log(board[11].piece.pin_mask);
                //     }
                //     Debug.Log(convertIndexToFieldName(move.start_square) + " -> " + convertIndexToFieldName(move.target_square)+" : "+tempNodes+"  depth: "+depth);
                // }
                if (bpieces != black_pieces_bitboard)
                {
                    DestroyVisual();
                    CreateVisualFromBoard();
                    break;
                }
                if (depth == fulldepth)
                {
                    Debug.Log(convertIndexToFieldName(move.start_square) + " -> " + convertIndexToFieldName(move.target_square) + " : " + tempNodes + "  depth: " + depth);
                }
                tempNodes = 0;
            }

            return nodes;
        }

        public ulong new_Perft(int depth)
        {
            if (depth == 0)
            {
                return 1;
            }

            List<Move> legal_moves = MoveGen();
            ulong nodes = 0;
            foreach (Move m in legal_moves)
            {
                make_move(m);
                nodes += new_Perft(depth - 1);
                undo_move(m);
            }
            return nodes;
        }

        public IEnumerator PerftVisual(int depth)
        {
            if (depth == 0)
                yield return null;


            List<Move> moves = MoveGen();

            foreach (Move move in moves)
            {
                make_move(move);
                DestroyVisual();
                CreateVisualFromBoard();
                yield return new WaitForSeconds(0.1f);
                undo_move(move);
                DestroyVisual();
                CreateVisualFromBoard();
                yield return new WaitForSeconds(0.1f);
            }


        }
        public string convertIndexToFieldName(int index)
        {
            char file = (char)('a' + (index % 8));
            char row = (char)('1' + (index / 8));
            string ans = "";
            ans += file;
            ans += row;
            return ans;
        }
        public int convertFieldNameToIndex(string s)
        {
            char letter = s[0];
            char number = s[1];
            letter -= 'a';
            number -= '1';
            int index = letter * 8 + number;
            return index;
        }
    }

}