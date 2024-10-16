﻿using Jypeli;
using System.Collections.Generic;

/// @author Antti Veli Matias Harju / anvemaha / anttharj
/// @version 12.11.2019
/// <summary>
/// Peli jossa palikoita tippuu jatkuvasti ja niitä sovitellaan vierekkäin
/// </summary>
#if WINDOWS
public class TippuvatPalikat : Game
#endif
#if ANDROID
public class TippuvatPalikatAndroidGame : Game
#endif
{
    private Timer updateTimer;
    private Timer automaticRestartTimer;

    private int score = 0;
    private int automaticRestartTime = 5;
    private int automaticRestartTimeReset;
    private int size;

    private Color backgroundColor = Color.Black;

    private static readonly int forcedShape = -1; //katso string[] shapes mikä numero vastaa mitä muotoa
    private int currentShape = -1;
    private int upcomingShape = -1;
    private int currentRotation = -1;

    private int[,] staticArray = new int[10, 24];
    private int[,] dynamicArray;
    private int[,] upcomingArray = new int[4, 4];

    private bool freefall = false;
    private bool spawn = false;
    private bool lost = false;

    private bool spawnHeldBlock = false;
    private bool updateHold = false;
    private int nextShape = 0;
    private bool tik = true;

    private static readonly int[] shapeArraySize = { 4, 2, 3, 3, 3, 3, 3 };
    private static readonly string[] shapeStartPositons = { "220", "421", "420", "321", "420", "421", "321" };
    private static readonly string[] shapes = { "stick", "block", "t", "worm", "corner", "wormR", "cornerR" };
    private static readonly string[] numberFont = { "111101101101111", "110010010010111", "111001111100111", "111001111001111", "101101111001001", "111100111001111", "111100111101111", "111001011001001", "111101111101111", "111101111001111" };
    private static readonly string restartText = "111011101110111011101110111101010001000010010101010010111011001110010011101110010110010000010010010101100010101011101110010010101010010";

    private List<string[]> shapeStrings = new List<string[]>();
    private List<Vector[]> shapeOffsets = new List<Vector[]>();
#if ANDROID
    private Touch activeTouch = null;
    private double movementTimer = 0;
    private double restartTimer = 0;
    private double rotateTimer = 0;
    private double downTimer = 0;
#endif
#if WINDOWS
    private int[,] holdArray = new int[4, 4];
    private int heldShape = -1;
    private bool canHold = true;
    private static readonly SoundEffect saveSound = LoadSoundEffect("pou");
#endif
    private static readonly SoundEffect leftSound = LoadSoundEffect("piu");
    private static readonly SoundEffect rightSound = LoadSoundEffect("pau");
    private static readonly SoundEffect rotateSound = LoadSoundEffect("pom");
    private static readonly SoundEffect downSound = LoadSoundEffect("pam");
    private static readonly SoundEffect destroySound = LoadSoundEffect("tuuuuf");
    private static readonly SoundEffect loseSound = LoadSoundEffect("pompompompoo");
    private static readonly SoundEffect restartSound = LoadSoundEffect("padam");
    private static readonly SoundEffect[] tiktokSound = { LoadSoundEffect("tik"), LoadSoundEffect("tok") };
    private static readonly SoundEffect[] pauseSound = { LoadSoundEffect("holdbreath"), LoadSoundEffect("letitgo") };


    /// <summary>
    /// Alustetaan peli kutsumalla alustusaliohjelmia ja 
    /// määritetään näppäimet
    /// </summary>
    public override void Begin()
    {
#if WINDOWS
        IsFullScreen = true;
        Mouse.IsCursorVisible = false;
#endif
        SetupGame();
        SetupArrays();
        SetupLoops();

        //Näppäimistö
        SetupDirections(Key.W, Key.A, Key.S, Key.D);
        SetupDirections(Key.Up, Key.Left, Key.Down, Key.Right);

        Keyboard.Listen(Key.P, ButtonState.Pressed, Pause, "Laita peli tauolle");
        Keyboard.Listen(Key.Space, ButtonState.Pressed, BrickToBottom, "Iske alas");
        Keyboard.Listen(Key.R, ButtonState.Pressed, Restart, "Aloita alusta");
        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä ohjeet");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, Exit, "Lopeta peli");

        //xbox ohjain
        ControllerOne.Listen(Button.DPadUp, ButtonState.Pressed, Rotate, "Kieritä palikkaa");
        ControllerOne.Listen(Button.DPadLeft, ButtonState.Pressed, MoveLeft, "Liiku vasemmalle");
        ControllerOne.Listen(Button.DPadDown, ButtonState.Pressed, FreefallToggle, "Nopeasti alas", true);
        ControllerOne.Listen(Button.DPadDown, ButtonState.Released, FreefallToggle, "", false);
        ControllerOne.Listen(Button.DPadRight, ButtonState.Pressed, MoveRight, "Liiku oikealle");

        ControllerOne.Listen(Button.Y, ButtonState.Pressed, Rotate, "Kieritä palikkaa");

        ControllerOne.Listen(Button.A, ButtonState.Pressed, BrickToBottom, "Iske alas");
        ControllerOne.Listen(Button.Start, ButtonState.Pressed, Restart, "Aloita alusta");
        ControllerOne.Listen(Button.Back, ButtonState.Pressed, Pause, "Laita peli tauolle");
#if WINDOWS
        Keyboard.Listen(Key.Q, ButtonState.Pressed, HoldBrick, "Ota palikka talteen");
        ControllerOne.Listen(Button.X, ButtonState.Pressed, HoldBrick, "Ota palikka talteen");
#endif
#if ANDROID
        TouchPanel.Listen(ButtonState.Down, TouchControls, "Liikuttaa pelaajaa");
        TouchPanel.Listen(ButtonState.Released, UnTouch, null);
        PhoneBackButton.Listen(Pause, "Laita peli tauolle");
#endif
        SpawnNextShape();
    }


#if ANDROID
    /// <summary>
    /// Kosketusnäyttöohjaukset android versiota varten
    /// </summary>
    /// <param name="kosketus">kosketus parametri</param>
    private void TouchControls(Touch kosketus)
    {
        if (activeTouch == null) activeTouch = kosketus;
        else if (kosketus != activeTouch) return;

        if (restartTimer <= 0 && activeTouch.PositionOnScreen.Y > 0)
        {
            Restart();
            restartTimer = 1;
        }

        if (movementTimer <= 0)
        {
            if (activeTouch.MovementOnWorld.X < -15)
            {
                MoveLeft();
                movementTimer = 0.03;
            }
            if (activeTouch.MovementOnWorld.X > 15)
            {
                MoveRight();
                movementTimer = 0.03;
            }
        }
        if (rotateTimer <= 0)
            if (activeTouch.MovementOnWorld.Y > 20)
            {
                Rotate();
                rotateTimer = 0.2;
            }
        if (activeTouch.MovementOnWorld.Y < -20)
        {
            Tiktok();
            MoveDown();
        }
    }


    /// <summary>
    /// Lopettaa kosketuksen
    /// </summary>
    /// <param name="kosketus">kosketus parametri</param>
    private void UnTouch(Touch kosketus)
    {
        if (kosketus == activeTouch) activeTouch = null;
    }


    /// <summary>
    /// Ajastimia eri kosketustoimintoja varten
    /// </summary>
    private void TouchLoop()
    {
        if (movementTimer > 0) movementTimer -= 0.01;
        if (restartTimer > 0) restartTimer -= 0.01;
        if (rotateTimer > 0) rotateTimer -= 0.01;
        if (downTimer > 0) downTimer -= 0.01;
    }
#endif


    /// <summary>
    /// Oma aliohjelma perusohjaukselle, että saan wasd ja nuolinäppäimet helposti
    /// yhtäaikaa käyttöön
    /// </summary>
    /// <param name="up">Näppäin jolla kiertää palikkaa</param>
    /// <param name="left">Näppäin jolla liikkua vasemmalle</param>
    /// <param name="down">Näppäin jolla tippua nopeasti</param>
    /// <param name="right">Näppäin jolla liikkua oikealle</param>
    private void SetupDirections(Key up, Key left, Key down, Key right)
    {
        Keyboard.Listen(up, ButtonState.Pressed, Rotate, "Kieritä palikkaa");
        Keyboard.Listen(left, ButtonState.Pressed, MoveLeft, "Liiku vasemmalle");
        Keyboard.Listen(down, ButtonState.Pressed, FreefallToggle, "Nopeasti alas", true);
        Keyboard.Listen(down, ButtonState.Released, FreefallToggle, "", false);
        Keyboard.Listen(right, ButtonState.Pressed, MoveRight, "Liiku oikealle");
    }


    /// <summary>
    /// Peli piirretään täällä
    /// </summary>
    /// <param name="canvas">pelin canvas</param>
    protected override void Paint(Canvas canvas)
    {
        //Pelikentän reunat
        canvas.BrushColor = NumberToColor(8);
        canvas.DrawLine(size * -5 - 1, size * -12, size * -5 - 1, size * 8);
        canvas.DrawLine(size * 5, size * -12, size * 5, size * 8);
        canvas.DrawLine(size * -5 - 2, size * -12 - 1, size * 5, size * -12 - 1);

        DrawArray(canvas, staticArray, Vector.Zero, true);
        DrawArray(canvas, dynamicArray, Vector.Zero);

        if (!lost)
        {
            //Tulevat palikat
#if ANDROID
            DrawArray(canvas, upcomingArray, new Vector(Screen.Width / 2, Screen.Height / 2), false, 2);
#endif
#if WINDOWS
            DrawArray(canvas, upcomingArray, new Vector(size * (staticArray.GetLength(0) + 1), size * (staticArray.GetLength(1) - (upcomingArray.GetLength(1) * 2))));
            DrawArray(canvas, holdArray, new Vector(-size * (holdArray.GetLength(0) + 1), size * (staticArray.GetLength(1) - (holdArray.GetLength(1) * 2))));
#endif
        }
        //Pistelaskuri (halusin tyylikkäästi skaalautuvan, siksi oma eikä joku valmis tekstipohjainen)
        string scoreText = score.ToString();

        for (int i = 0; i < scoreText.Length; i++)
            DrawTextWithBackground(canvas, numberFont[scoreText[i] - 48], 5, 3, new Vector(-Screen.Width / 2, Screen.Height / 2 - 3 * size), i);

        if (lost)
        {
            double yPos = Screen.Height / 2 - 6 * size;
            DrawTextWithBackground(canvas, restartText, 5, 27, new Vector(-28 * size / 4, yPos));
            DrawTextWithBackground(canvas, numberFont[automaticRestartTime], 5, 3, new Vector(-4 * size / 4, yPos - 3 * size));
        }

        base.Paint(canvas);
    }


    /// <summary>
    /// Piirtää kaksiulotteisen taulukon halutulle canvakselle halutussa kohdassa
    /// </summary>
    /// <param name="canvas">Minne piirretään</param>
    /// <param name="array">Mikä piirretään</param>
    /// <param name="position">Kohta josta aloitetaan piirtämään</param>
    /// <param name="drawBackground">Taustaväri</param>
    private void DrawArray(Canvas canvas, int[,] array, Vector position, bool drawBackground = false, int sizeDivider = 1)
    {
        int xOffset = size * -5 / sizeDivider;
        int yOffset = size * -12 / sizeDivider;

        for (int x = 0; x < array.GetLength(0); x++)
            for (int y = 0; y < array.GetLength(1); y++)
            {
                canvas.BrushColor = NumberToColor(array[x, y]);

                int scaledX = x * size / sizeDivider;
                int scaledY = y * size / sizeDivider;

                int scaledSize = 0;
                if (sizeDivider != 1) scaledSize = size / (sizeDivider * 2);

                if (array[x, y] != 0) DrawCube(canvas, new Vector(scaledX + (int)position.X + xOffset, scaledY + (int)position.Y + yOffset), scaledSize);

                if (drawBackground)
                    if (y < 20 || array[x, y] != 0)
                        if (array[x, y] == 0) //piirtää taustalle ruudukon
                            if (!IsAnythingAbove(new Vector(x, y), dynamicArray) || IsAnythingAbove(new Vector(x, y), staticArray))
                            {
                                canvas.BrushColor = NumberToColor(8);
                                DrawCube(canvas, new Vector(scaledX + xOffset, scaledY + yOffset));
                                canvas.BrushColor = backgroundColor;
                                DrawCube(canvas, new Vector(scaledX + xOffset, scaledY + yOffset), 1);
                            }
            }
    }


    /// <summary>
    /// Jypelissä ei ollut valmista funktiota jolla piirtää
    /// kuutio, joten tässä oma joka vain piirtää monta viivaa vierekkäin
    /// </summary>
    /// <param name="canvas">Minne piirretään</param>
    /// <param name="position">Mihin kohtaan piirretään</param>
    /// <param name="offset">Kutistaa kuution kokoa</param>
    private void DrawCube(Canvas canvas, Vector position, int offset = 0)
    {
        for (int i = 0 + offset; i < size - offset; i++) canvas.DrawLine(position.X + i, position.Y + offset, position.X + i, position.Y + size - offset);
    }


    /// <summary>
    /// Piirtää merkkijonosta jonkinlaisen merkin annettujen parametrien pohjalta
    /// </summary>
    /// <param name="canvas">Minne piirretään</param>
    /// <param name="s">merkkijono johon tallennettu jotain tekstiä</param>
    /// <param name="height">tekstin korkeus (laatikoissa)</param>
    /// <param name="width">tekstin leveys (laatikoissa)</param>
    /// <param name="start">Mihin kohtaan ruutua teksti piirretään</param>
    /// <param name="xOffset">Käytetään numeroja piirrettessä, käytännössä monesko numero numero on</param>
    /// <param name="sizeDivider">voidaan muuttaa tekstin "lihavuutta", isompi arvo tekee lihavemman tekstin</param>
    private void DrawText(Canvas canvas, string s, int height, int width, Vector start, int xOffset = 0, int sizeDivider = 4)
    {
        int index = 0;

        for (int y = height - 1; y >= 0; y--)
            for (int x = 0; x < width; x++)
            {
                if (s[index] == '1') DrawCube(canvas, start + new Vector(x * size / 2 + size * 2 * xOffset, y * size / 2), size / sizeDivider);
                index++;
            }
    }


    /// <summary>
    /// Sama kuin DrawText, mutta piirtää mustat ääriviivat
    /// </summary>
    /// <param name="canvas">Minne piirretään</param>
    /// <param name="s">Merkkijono jossa tekstiä</param>
    /// <param name="height">tekstin korkeus</param>
    /// <param name="width">tekstin leveys</param>
    /// <param name="start">Mihin kohtaan ruutua teksti piirretään</param>
    private void DrawTextWithBackground(Canvas canvas, string s, int height, int width, Vector start, int xOffset = 0)
    {
        canvas.BrushColor = backgroundColor;
        DrawText(canvas, s, height, width, start, xOffset, 5);
        canvas.BrushColor = Color.White;
        DrawText(canvas, s, height, width, start, xOffset, 4);
    }


    /// <summary>
    /// Pelilogiikka pyörii täällä, voisi kai tehdä jotenkin overriden avulla, mutta tein nyt näin.
    /// </summary>
    private void Update()
    {
        if (spawn || spawnHeldBlock)
        {
            if (spawnHeldBlock)
            {
                SpawnNextShape(nextShape, updateHold);
                nextShape = 0;
                updateHold = true;
                spawnHeldBlock = false;
#if WINDOWS
                canHold = false;
#endif
            }
            if (spawn)
            {
                SpawnNextShape();
                spawn = false;
            }
        }
        else
        {
            if (!lost)
            {
                Tiktok();
                MoveDown();
            }
        }
    }


    /// <summary>
    /// Timeri kutsuu tätä kerran sekunnissa pelaajan hävittyä pelin
    /// Käynnistää pelin automaattisesti uudelleen
    /// </summary>
    private void AutomaticRestart()
    {
        if (lost)
        {
            if (automaticRestartTime > 1)
                automaticRestartTime -= 1;
            else
                Restart();

            tiktokSound[0].Play();
        }
    }


    /// <summary>
    /// Liikuttaa dynamicarrayta yhdellä alas ja antaa pisteitä täysistä riveistä
    /// </summary>
    /// <returns>Tosi jos rivi tuli täyteen</returns>
    private bool MoveDown()
    {
        if (CanMoveWholeArray(dynamicArray, staticArray)) dynamicArray = MoveWholeArrayDown(dynamicArray);
        else
        {
            downSound.Play();
            staticArray = CombineArrays(staticArray, dynamicArray);
            CheckForFullLines();
            SetArrayToZero(dynamicArray);
            CheckIfLost();
            return true;
        }

        return false;
    }


    /// <summary>
    /// Luo taustalle tikittävää ääntä, ei haluta käyttää koko aika samaa ääntä, että siinä on tietynlainen rytmi
    /// </summary>
    private void Tiktok()
    {
        tik = !tik;

        if (tik)
            tiktokSound[0].Play();
        else
            tiktokSound[1].Play();
    }


    /// <summary>
    /// Timerilla tehty looppi, halusin pystyä hienosäätämään tippumisen nopeutta.
    /// </summary>
    private void FreefallLoop()
    {
        if (freefall && !lost)
        {
            Tiktok();
            bool stop = MoveDown();
            if (stop)
                freefall = false;
        }
    }


    /// <summary>
    /// Palauttaa numeroa vastaavan värin, 8 palauttaa nykyisen palikan värin jos peli ei ole loppunut ja tummanharmaan jos on
    /// </summary>
    /// <param name="n">numero</param>
    /// <returns>värin</returns>
    private Color NumberToColor(int n)
    {
        Color[] colors = { Color.White, Color.Cyan, Color.Yellow, Color.Purple, Color.Green, Color.Blue, Color.Red, Color.Orange, Color.DarkGray };

        if (n == 8 && !lost) return colors[currentShape + 1];

        return colors[n];
    }


    /// <summary>
    /// Muuttaa string muuttujan 2-ulotteiseksi int taulukoksi.
    /// Helpompi säilöä string muuttujissa palikoiden eri kieritysasentoja kuin suoraan taulukoissa.
    /// </summary>
    /// <param name="s">string josta muutetaan taulukko</param>
    /// <param name="rowLength">Rivin leveys</param>
    /// <returns>2-ulotteisen int taulukon</returns>
    public static int[,] StringTo2DArray(string s, int rowLength)
    {
        int[,] array = new int[rowLength, rowLength];

        for (int x = 0; x < array.GetLength(0); x++)
            for (int y = 0; y < array.GetLength(1); y++)
                array[x, y] = s[y * rowLength + x] - 48; //array[x, y] = int.Parse(Char.ToString(s[y * size + x])); //Toimis kans mutta mitä turhia

        return array;
    }


    /// <summary>
    /// Löytää taulukosta vasemmalta oikealle, alhaalta ylös etsien ensimmäisen arvon joka ei ole nolla ja palauttaa sen sijainnin
    /// </summary>
    /// <param name="array">Taulukko, josta etsitään</param>
    /// <returns>Ensimmäisen ei 0 arvoisen muuttujan sijainnin, (-1,-1) jos mitään ei löydetty</returns>
    public static Vector FindStartPosition(int[,] array)
    {
        Vector start = -Vector.One;

        for (int x = 0; x < array.GetLength(0); x++) for (int y = 0; y < array.GetLength(1); y++)
                if (array[x, y] != 0)
                    return new Vector(x, y);

        return start;
    }


    /// <summary>
    /// Liimaa small taulukon big taulukon päälle annetussa sijainnissa
    /// </summary>
    /// <param name="small">liimattava taulukko</param>
    /// <param name="big">taulukko johon liimataan</param>
    /// <param name="position">liimauksen aloituskohta</param>
    /// <returns></returns>
    public static int[,] AddArrayToArrayAtPosition(int[,] small, int[,] big, Vector position)
    {
        //Kopiodaan big -> final
        int[,] final = new int[big.GetLength(0), big.GetLength(1)];

        for (int x = 0; x < small.GetLength(0); x++) for (int y = 0; y < small.GetLength(1); y++)
                final[x, y] = big[x, y];

        //Vältetään index out of bounds virheet (a godsend)
        if (position.X + small.GetLength(0) > big.GetLength(0)) position.X = big.GetLength(0) - small.GetLength(0);
        if (position.Y + small.GetLength(1) > big.GetLength(1)) position.Y = big.GetLength(1) - small.GetLength(1);
        if (position.X < 0) position.X = 0;
        if (position.Y < 0) position.Y = 0;

        //Liimataan
        for (int x = 0; x < small.GetLength(0); x++) for (int y = 0; y < small.GetLength(1); y++)
                final[(int)position.X + x, (int)position.Y + y] = small[x, y];

        return final;
    }


    /// <summary>
    /// Pysäyttää pelin hienojen ääniefektien kera
    /// </summary>
    private void Pause()
    {
        IsPaused = !IsPaused;

        if (IsPaused)
            pauseSound[0].Play();
        else
            pauseSound[1].Play();
    }


    /// <summary>
    /// Aloittaa pelin alusta
    /// </summary>
    private void Restart()
    {
        IsPaused = false;
        automaticRestartTimer.Stop();
        restartSound.Play();
        updateTimer.Stop();
        updateTimer.Start();
        lost = false;
        SetArrayToZero(dynamicArray);
        SetArrayToZero(staticArray);
        currentShape = RandomGen.NextInt(shapes.Length);
        upcomingShape = RandomGen.NextInt(shapes.Length);
#if WINDOWS
        SetArrayToZero(holdArray);
        heldShape = -1;
#endif
        score = 0;
        automaticRestartTime = automaticRestartTimeReset;
        SpawnNextShape();
    }


    /// <summary>
    /// Kytkee nopean tippumisen päälle / pois päältä
    /// </summary>
    private void FreefallToggle(bool state)
    {
        freefall = state;
    }


#if WINDOWS
    /// <summary>
    /// Ottaa talteen nykyisen palikan ja jos tallessa on jo palikkaa antaa sen käytettäväksi.
    /// Ei ehkä kuvaava nimi, but this does put a smile on my face.
    /// </summary>
    private void HoldBrick()
    {
        if (canHold && !lost && heldShape != currentShape)
        {
            saveSound.Play();
            nextShape = -1;
            updateHold = true;

            if (heldShape == -1)
            {
                heldShape = currentShape;
                holdArray = AddArrayToArrayAtPosition(StringTo2DArray(shapeStrings[heldShape][0], shapeArraySize[heldShape]), holdArray, Vector.Zero);
            }
            else
            {
                nextShape = heldShape;
                heldShape = currentShape;
                holdArray = AddArrayToArrayAtPosition(StringTo2DArray(shapeStrings[heldShape][0], shapeArraySize[heldShape]), holdArray, Vector.Zero);
                currentShape = nextShape;
                updateHold = false;
            }

            SetArrayToZero(dynamicArray);
            spawnHeldBlock = true;
            canHold = false;
        }
    }
#endif


    /// <summary>
    /// Läimäyttää nykyisen palikan alas.
    /// </summary>
    private void BrickToBottom()
    {
        if (!lost)
            if (FindStartPosition(dynamicArray) != -Vector.One) //pitää olla jotakin mitä liikuttaa alas tai jäädään jumiin
                while (true)
                    if (MoveDown()) break;
    }


    /// <summary>
    /// Kiertää tippuvaa palikkaa
    /// </summary>
    private void Rotate()
    {
        if (!lost)
            if ((IsArrayEmpty(dynamicArray) || CanRotate(dynamicArray, staticArray, currentRotation, currentShape, shapeStrings, shapeArraySize, shapeOffsets)) && currentShape != 1)
            {
                var result = RotateInArray(dynamicArray, currentRotation, currentShape, shapeStrings, shapeArraySize, shapeOffsets);
                dynamicArray = result.array;
                currentRotation = result.currentRotation;
                rotateSound.Play();
            }
    }


    /// <summary>
    /// Onko taulukko tyhjä
    /// </summary>
    /// <param name="array">Tarkistettava taulukko</param>
    /// <returns>Tosi, jos on tyhjä, epätosi jos ei</returns>
    public static bool IsArrayEmpty(int[,] array)
    {
        bool isEmpty = true;

        for (int x = 0; x < array.GetLength(0); x++)
            for (int y = 0; y < array.GetLength(1); y++)
                if (array[x, y] != 0)
                    isEmpty = false;

        return isEmpty;
    }


    /// <summary>
    /// Tarkistaa, onko palikkaa mahdollista pyöräyttää kopioimalla dynamicArrayn uuteen taulukkoon, pyöräyttämällä siinä ja tarkistamalla meneekö päällekkäin staticArrayn kanssa
    /// Pakko olla public static eikä private, koska muuten tulee sellasta ikävän paljon globaali muuttuja muistuttavaa säätöä josta tulee outoja bugeja joiden kanssa en halua tapella.
    /// Sen takia mahdoton aliohjelmakutsussa mahdoton määrä muuttujia
    /// </summary>
    /// <param name="rotationArray">Pyöräytettävä taulukko</param>
    /// <param name="staticArray">Taulukko, jossa on palikat jotka eivät enää tipu</param>
    /// <param name="currentRotation">Tippuvan palikan asento</param>
    /// <param name="currentShape">Tippuvan palikan muoto</param>
    /// <param name="shapeStrings">Kaikki palikat ja niiden asennot</param>
    /// <param name="shapeArraySize">Palikoiden koot taulukoksi muuttamista varten</param>
    /// <param name="shapeOffsets">Tänne on säilötty paljonko palikoita pitää eri pyöräytysvaiheiden välillä liikuttaa, ettei niillä voi lentää ylöspäin tai liusu sivuille</param>
    /// <returns>Voidaanko pyöräyttää vai ei</returns>
    public static bool CanRotate(int[,] rotationArray, int[,] staticArray, int currentRotation, int currentShape, List<string[]> shapeStrings, int[] shapeArraySize, List<Vector[]> shapeOffsets)
    {
        //kopiodaan rotationArray omaan newArray:yn
        int[,] newArray = new int[rotationArray.GetLength(0), rotationArray.GetLength(1)];

        for (int x = 0; x < rotationArray.GetLength(0); x++) for (int y = 0; y < rotationArray.GetLength(1); y++)
                newArray[x, y] = rotationArray[x, y];

        var result = RotateInArray(newArray, currentRotation, currentShape, shapeStrings, shapeArraySize, shapeOffsets);
        newArray = result.array;

        return !ArraysOverlap(newArray, staticArray);
    }


    /// <summary>
    /// Piti pystyä palauttamaan kaksi eri arvoa aliohjelmasta, tämä kai oikea tapa siihen
    /// </summary>
    public struct RotationResult
    {
        public int[,] array;
        public int currentRotation;
    }


    /// <summary>
    /// Pyöräytetään annettussa taulukossa (rotationArray) olevaa palikkaa
    /// </summary>
    /// <param name="rotationArray">Taulukko jossa pyöräytetään</param>
    /// <param name="currentRotation">Tippuvan palikan asento</param>
    /// <param name="currentShape">Tippuvan palikan muoto</param>
    /// <param name="shapeStrings">Kaikki palikat ja niiden asennot</param>
    /// <param name="shapeArraySize">Palikoiden koot taulukoksi muuttamista varten</param>
    /// <param name="shapeOffsets">Tänne on säilötty paljonko palikoita pitää eri pyöräytysvaiheiden välillä liikuttaa, ettei niillä voi lentää ylöspäin tai liusu sivuille</param>
    /// <returns>Taulukon jossa palikkaa on pyöräytetty kerran</returns>
    public static RotationResult RotateInArray(int[,] rotationArray, int currentRotation, int currentShape, List<string[]> shapeStrings, int[] shapeArraySize, List<Vector[]> shapeOffsets)
    {
        Vector pos = FindStartPosition(rotationArray);

        if (pos != new Vector(-1, -1))
        {
            pos += shapeOffsets[currentShape][currentRotation]; //Ennen tehtiin RotationOffset aliohjelmassa, oli niin lyhyt ja vain tässä käytetty joten liitin suoraan tähän

            SetArrayToZero(rotationArray);
            currentRotation++;

            if (currentRotation >= shapeStrings[currentShape].Length) currentRotation = 0;

            rotationArray = AddArrayToArrayAtPosition(StringTo2DArray(shapeStrings[currentShape][currentRotation], shapeArraySize[currentShape]), rotationArray, new Vector((int)pos.X, (int)pos.Y));
        }

        return new RotationResult { array = rotationArray, currentRotation = currentRotation };
    }


    /// <summary>
    /// Onko kahdessa eri (samankokoisessa) taulukossa kohtia joissa on samassa kohdassa joku muu arvo kuin nolla
    /// </summary>
    /// <param name="a">1. taulukko</param>
    /// <param name="b">2. taulukko</param>
    /// <returns>Tosi, jos on kohta jossa arvoja menee päällekkäin</returns>
    public static bool ArraysOverlap(int[,] a, int[,] b)
    {
        for (int x = 0; x < a.GetLength(0); x++) for (int y = 0; y < a.GetLength(1); y++)
                if (a[x, y] != 0)
                    if (b[x, y] != 0)
                        return true;
        return false;
    }


    /// <summary>
    /// Liikuttaa dynamicArrayta yhdella vasemmalle
    /// </summary>
    private void MoveLeft()
    {
        if (CanMoveHorizontally(staticArray, dynamicArray, 0, -1) && !lost)
        {
            leftSound.Play();
            for (int x = 0; x < dynamicArray.GetLength(0); x++) for (int y = 0; y < dynamicArray.GetLength(1); y++)
                {
                    if (x == dynamicArray.GetLength(0) - 1)
                        dynamicArray[x, y] = 0;
                    else
                        dynamicArray[x, y] = dynamicArray[x + 1, y];
                }
        }
    }


    /// <summary>
    /// Liikuttaa dynamicArrayta yhdellä oikealle
    /// </summary>
    private void MoveRight()
    {
        if (CanMoveHorizontally(staticArray, dynamicArray, 9, 1) && !lost)
        {
            rightSound.Play();

            for (int x = dynamicArray.GetLength(0) - 1; x >= 0; x--) for (int y = 0; y < dynamicArray.GetLength(1); y++)
                {
                    if (x == 0)
                        dynamicArray[x, y] = 0;
                    else
                        dynamicArray[x, y] = dynamicArray[x - 1, y];
                }
        }
    }


    /// <summary>
    /// Tarkistaa voidaanko liikkua sivusuunnassa
    /// </summary>
    /// <param name="staticArray">Taulukko, jossa on palikat jotka ei enää tipu</param>
    /// <param name="dynamicArray">Taulukko, jossa on tippuva palikka</param>
    /// <param name="startX">Kummasta reunasta aloitetaan (0 vai 9)</param>
    /// <param name="direction">Suunta</param>
    /// <returns>Tosi jos annettuun suuntaa liikkuessa ei ole mitään tiellä</returns>
    public static bool CanMoveHorizontally(int[,] staticArray, int[,] dynamicArray, int startX, int direction)
    {
        //ei mennä reunojen yli
        for (int i = 0; i < dynamicArray.GetLength(1); i++)
            if (dynamicArray[startX, i] != 0)
                return false;

        for (int x = 0; x < dynamicArray.GetLength(0); x++) for (int y = 0; y < dynamicArray.GetLength(1); y++)
                if (dynamicArray[x, y] != 0)
                    if (staticArray[x + direction, y] != 0)
                        return false;

        return true;
    }


    /// <summary>
    /// Tarkistaa voiko koko dynamicArray liikkua yhdellä alas
    /// </summary>
    /// <param name="dynamicArray">Taulukko, jossa tippuva palikka</param>
    /// <param name="staticArray">Taulukko, jossa tippuneet palikat</param>
    /// <returns>Tosi, jos voidaan liikkua yhdellä alas</returns>
    public static bool CanMoveWholeArray(int[,] dynamicArray, int[,] staticArray)
    {
        for (int x = 0; x < dynamicArray.GetLength(0); x++) for (int y = 0; y < dynamicArray.GetLength(1); y++)
                if (dynamicArray[x, y] != 0)
                    if (y == 0 || staticArray[x, y - 1] != 0)
                        return false;
        return true;
    }


    /// <summary>
    /// Liikuttaa koko dynamicArrayta yhdellä alas
    /// </summary>
    /// <param name="dynamicArray">Taulukko, jossa tippuva palikka</param>
    /// <returns>Taulukon, jossa liikuttu yhdellä alas</returns>
    public static int[,] MoveWholeArrayDown(int[,] dynamicArray)
    {
        int[,] newArray = new int[dynamicArray.GetLength(0), dynamicArray.GetLength(1)];

        for (int x = 0; x < dynamicArray.GetLength(0); x++) for (int y = 1; y < dynamicArray.GetLength(1); y++)
            {
                newArray[x, y] = 0;
                if (dynamicArray[x, y] != 0) newArray[x, y - 1] = dynamicArray[x, y];
            }

        return newArray;
    }


    /// <summary>
    /// Yhdistää kaksi taulukkoa uuteen taulukkoon
    /// </summary>
    /// <param name="a">taulukko 1</param>
    /// <param name="b">taulukko 2</param>
    /// <returns>taulukko 1 ja 2 yhdistelmän</returns>
    public static int[,] CombineArrays(int[,] a, int[,] b)
    {
        int[,] c = new int[a.GetLength(0), a.GetLength(1)];

        for (int x = 0; x < a.GetLength(0); x++) for (int y = 0; y < a.GetLength(1); y++)
            {
                if (a[x, y] != 0)
                    c[x, y] = a[x, y];
                if (b[x, y] != 0)
                    c[x, y] = b[x, y];
            }

        return c;
    }


    /// <summary>
    /// Tarkistaa onko staticArrayssa täysiä rivejä, tuhoaa ne ja antaa jokaisesta tuhotusta pisteen
    /// </summary>
    private void CheckForFullLines()
    {
        List<int> linesToRemove = new List<int>();

        for (int y = 0; y < staticArray.GetLength(1); y++)
        {
            bool full = true;
            for (int x = 0; x < staticArray.GetLength(0); x++)
                if (staticArray[x, y] == 0) full = false;
            if (full)
            {
                destroySound.Play();
                DestroyLine(y);
                score++; //sait pisteen

                linesToRemove.Add(y);
            }
        }

        for (int i = 0; i < linesToRemove.Count; i++) MoveDownFromY(linesToRemove[i] - i);
    }


    /// <summary>
    /// Asettaa annetussa kohdassa olevan vaakasuoran rivin tyhjäksi
    /// </summary>
    /// <param name="y"></param>
    private void DestroyLine(int y)
    {
        for (int x = 0; x < staticArray.GetLength(0); x++)
            staticArray[x, y] = 0;
    }


    /// <summary>
    /// Liikuttaa annetun kohdan yläpuolella olevia rivejä yhdellä alas
    /// </summary>
    /// <param name="startY"></param>
    private void MoveDownFromY(int startY)
    {
        for (int x = 0; x < staticArray.GetLength(0); x++) for (int y = startY; y < staticArray.GetLength(1) - 1; y++)
                staticArray[x, y] = staticArray[x, y + 1];
    }


    /// <summary>
    /// Tarkistaa onko palikoita tippunut yli pelikentän laitojen
    /// </summary>
    private void CheckIfLost()
    {
        bool didLose = false;

        for (int i = 0; i < 10; i++)
            if (staticArray[i, 20] != 0)
                didLose = true;

        if (didLose) //hävisit pelin
        {
            loseSound.Play();
            lost = true;
            automaticRestartTimer.Start();
        }
        else
        {
            spawn = true;
            currentRotation = 0;
        }
    }


    /// <summary>
    /// Täyttää annetun taulukon nollilla
    /// </summary>
    /// <param name="array">muokattava taulukko</param>
    public static void SetArrayToZero(int[,] array, int number = 0)
    {
        for (int x = 0; x < array.GetLength(0); x++) for (int y = 0; y < array.GetLength(1); y++)
                array[x, y] = number;
    }


    /// <summary>
    /// Luo uuden satunnaisen palikan pelikentälle
    /// </summary>
    /// <param name="specificShape">Käytä jos haluat luoda tietyn palikan</param>
    /// <param name="updateUpcomingArray">Päivitetäänkö tuleva palikka</param>
    private void SpawnNextShape(int specificShape = 0, bool updateUpcomingArray = true)
    {
        if (specificShape != 0) currentShape = specificShape;

        if (updateUpcomingArray)
        {
            if (forcedShape == -1)
            {
                currentShape = upcomingShape;
                upcomingShape = RandomGen.NextInt(shapes.Length);
            }
            else
            {
                currentShape = forcedShape;
                upcomingShape = forcedShape;
            }

            upcomingArray = AddArrayToArrayAtPosition(StringTo2DArray(shapeStrings[upcomingShape][0], shapeArraySize[upcomingShape]), upcomingArray, Vector.Zero);
        }

        SpawnSpecificShape(shapes[currentShape]);
#if WINDOWS
        canHold = true;
#endif
    }


    /// <summary>
    /// Luo uuden halutun palikan pelikentälle
    /// </summary>
    /// <param name="shape">Halutun palikan numero</param>
    private void SpawnSpecificShape(string shape = "")
    {
        currentRotation = 0;

        for (int i = 0; i < shapes.Length; i++)
            if (shape == shapes[i])
                dynamicArray = AddArrayToArrayAtPosition(StringTo2DArray(shapeStrings[i][0], shapeArraySize[i]), dynamicArray, SpawnStartPos(shapeStartPositons[i]));
    }


    /// <summary>
    /// Ottaa tiedon string tyyppisestä muuttujasta int muuttujaan palikan aloitussijainnista
    /// </summary>
    /// <param name="s">muuttuja jossa tieto aloitussijainnista</param>
    /// <returns>sijainnin numerona</returns>
    public static Vector SpawnStartPos(string s)
    {
        return new Vector(s[0] - 48, (s[1] - 48) * 10 + (s[2] - 48));
    }


    /// <summary>
    /// Alustetaan peli
    /// </summary>
    private void SetupGame()
    {
        size = (int)Screen.Height / 30;

        string[] stick = { "0010001000100010", "0000000011110000" };
        string[] block = { "2222" };
        string[] t = { "000333030", "030033030", "030333000", "030330030" };
        string[] worm = { "044440000", "400440040" };
        string[] corner = { "000555005", "055050050", "500555000", "050050550" };
        string[] wormR = { "660066000", "060660600" };
        string[] cornerR = { "777700000", "700700770", "000007777", "770070070" };

        Vector[] stickOffset = { new Vector(-1, -1), new Vector(-1, -1) };
        Vector[] blockOffset = { new Vector(0, 0) };
        Vector[] tOffset = { new Vector(0, -1), new Vector(-1, 0), new Vector(0, -1), new Vector(0, -1) };
        Vector[] wormOffset = { new Vector(0, -1), new Vector(0, 0) };
        Vector[] cornerOffset = { new Vector(0, 0), new Vector(0, 0), new Vector(0, -1), new Vector(-1, -2) };
        Vector[] wormROffset = { new Vector(0, 0), new Vector(0, -1) };
        Vector[] cornerROffset = { new Vector(0, -1), new Vector(-1, 0), new Vector(1, -1), new Vector(0, 0) };

        shapeStrings = new List<string[]>() { stick, block, t, worm, corner, wormR, cornerR };
        shapeOffsets = new List<Vector[]>() { stickOffset, blockOffset, tOffset, wormOffset, cornerOffset, wormROffset, cornerROffset };

        Level.BackgroundColor = backgroundColor;

        if (forcedShape == -1)
            upcomingShape = RandomGen.NextInt(shapes.Length);
        else
            upcomingShape = forcedShape;

        automaticRestartTimeReset = automaticRestartTime;
    }


    /// <summary>
    /// Alustetaan timerit looppeja varten
    /// </summary>
    private void SetupLoops()
    {
        updateTimer = new Timer { Interval = 0.3 };
        updateTimer.Timeout += Update;
        updateTimer.Start();

        automaticRestartTimer = new Timer { Interval = 1 };
        automaticRestartTimer.Timeout += AutomaticRestart;

        Timer freefallTimer = new Timer { Interval = 0.03 };
        freefallTimer.Timeout += FreefallLoop;
        freefallTimer.Start();
#if ANDROID
        Timer touchTimer = new Timer { Interval = 0.01 };
        touchTimer.Timeout += TouchLoop;
        touchTimer.Start();
#endif
    }


    /// <summary>
    /// Alustetaan taulukot
    /// </summary>
    private void SetupArrays()
    {
        staticArray = new int[staticArray.GetLength(0), staticArray.GetLength(1)];
        dynamicArray = new int[staticArray.GetLength(0), staticArray.GetLength(1)];
    }


    /// <summary>
    /// Tarkistaa onko annetun sijainnin yläpuolella annetussa taulukossa jotain muuta kuin 0
    /// </summary>
    /// <param name="position">Sijainti</param>
    /// <param name="array">Taulukko</param>
    /// <returns>Tosi, jos sijainnin yläpuolella taulukossa on jotain muuta kuin 0</returns>
    public static bool IsAnythingAbove(Vector position, int[,] array)
    {
        for (int y = (int)position.Y; y < array.GetLength(1); y++)
            if (array[(int)position.X, y] != 0)
                return true;

        return false;
    }
}
