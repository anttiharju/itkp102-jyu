using Jypeli;
using System.Collections.Generic;

public class Tetris : Game
{
    private int score = 0;
    private int size;
    private readonly int forcedShape = 0;

    private Color backgroundColor = Color.Black;

    private int currentShape = 0;
    private int upcomingShape = 0;
    private int heldShape = -1;
    private int currentRotation = 0;

    private int[,] staticArray = new int[10, 24];
    private int[,] dynamicArray;
    private int[,] upcomingArray = new int[4, 4];
    private int[,] holdArray = new int[4, 4];

    private bool freefall = false;
    private bool spawn = false;
    private bool canHold = true;
    private bool lost = false;

    private bool spawnHeldBlock = false;
    private bool updateHold = false;
    private int nextShape = 0;

    private readonly int[] shapeArraySize = { 4, 2, 3, 3, 3, 3, 3 };
    private readonly string[] shapeStartPositons = { "220", "421", "420", "321", "421", "421", "321" };
    private readonly string[] shapes = { "stick", "block", "t", "worm", "corner", "wormR", "cornerR" };
    private readonly string[] numberFont = { "111101101101111", "111010010010110", "111100111001111", "111001011001111", "001001111101101", "111001111100111", "111101111100111", "001001011001111", "111101111101111", "111001111101111" };

    private List<string[]> shapeStrings = new List<string[]>();
    private List<Vector[]> shapeOffsets = new List<Vector[]>();

    public override void Begin()
    {
        size = (int)Screen.Height / 28;

        string[] stick = { "0010001000100010", "0000000011110000" };
        string[] block = { "2222" };
        string[] t = { "000333030", "030033030", "030333000", "030330030" };
        string[] worm = { "044440000", "400440040" };
        string[] corner = { "000555005", "055050050", "500555000", "050050550" };
        string[] wormR = { "660066000", "060660600" };
        string[] cornerR = { "777700000", "700700770", "000007777", "770070070" };

        Vector[] stickOffset = { new Vector(-1, -1), new Vector(-1, -1) };
        Vector[] blockOffset = { new Vector(0, 0) };
        Vector[] tOffset = { new Vector(0, -1), new Vector(-1, 0), new Vector(-1, -1), new Vector(-1, -1) };
        Vector[] wormOffset = { new Vector(-1, -1), new Vector(0, 0) };
        Vector[] cornerOffset = { new Vector(0, 0), new Vector(0, 0), new Vector(0, -1), new Vector(-2, -2) };
        Vector[] wormROffset = { new Vector(0, 0), new Vector(-1, -1) };
        Vector[] cornerROffset = { new Vector(0, -1), new Vector(-1, 0), new Vector(-1, -1), new Vector(0, 0) };

        shapeStrings = new List<string[]>() { stick, block, t, worm, corner, wormR, cornerR };
        shapeOffsets = new List<Vector[]>() { stickOffset, blockOffset, tOffset, wormOffset, cornerOffset, wormROffset, cornerROffset };

        SetupArrays();
        SetupLoops();
        SetupGame();

        SetupDirections(Key.W, Key.A, Key.S, Key.D);
        SetupDirections(Key.Up, Key.Left, Key.Down, Key.Right);

        Keyboard.Listen(Key.Space, ButtonState.Pressed, SlamDown, "Iske alas");
        Keyboard.Listen(Key.Q, ButtonState.Pressed, HoldUp, "Ota palikka talteen");
        Keyboard.Listen(Key.R, ButtonState.Pressed, Restart, "Aloita alusta");

        SpawnNextShape();

        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä ohjeet");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, Exit, "Lopeta peli");
    }

    private void SetupDirections(Key up, Key left, Key down, Key right)
    {
        Keyboard.Listen(up, ButtonState.Pressed, Rotate, "Kieritä palikkaa");
        Keyboard.Listen(left, ButtonState.Pressed, MoveLeft, "Liiku vasemmalle");
        Keyboard.Listen(down, ButtonState.Pressed, FreefallOn, "Nopeasti alas");
        Keyboard.Listen(down, ButtonState.Released, FreefallOff, "");
        Keyboard.Listen(right, ButtonState.Pressed, MoveRight, "Liiku oikealle");
    }

    /// <summary>
    /// Piirtää pelikentän
    /// </summary>
    protected override void Paint(Canvas canvas)
    {
        //Pelikenttä
        canvas.BrushColor = NumberToColor(currentShape + 1);
        canvas.DrawLine(size * -5 - 1, size * -12, size * -5 - 1, size * 8);
        canvas.DrawLine(size * 5, size * -12, size * 5, size * 8);
        canvas.DrawLine(size * -5 - 2, size * -12 - 1, size * 5, size * -12 - 1);

        DrawArray(canvas, staticArray, 0, 0, true);
        DrawArray(canvas, dynamicArray, 0, 0);

        //Tulevat palikat
        DrawArray(canvas, upcomingArray, size * (staticArray.GetLength(0) + 1), size * (staticArray.GetLength(1) - (upcomingArray.GetLength(1) * 2)));

        //Tallennettu palikka
        DrawArray(canvas, holdArray, -size * (holdArray.GetLength(0) + 1), size * (staticArray.GetLength(1) - (holdArray.GetLength(1) * 2)));

        //Pistelaskuri (halusin tyylikkäästi skaalautuvan, siksi oma eikä joku valmis tekstipohjainen)
        canvas.BrushColor = NumberToColor(0);
        string scoreText = score.ToString();

        for (int i = 0; i < scoreText.Length; i++)
        {
            DrawNumber(canvas, scoreText[i] - 48, i);
        }

        base.Paint(canvas);
    }


    private void DrawArray(Canvas canvas, int[,] array, int positionX, int positionY, bool drawBackground = false)
    {
        int xOffset = size * -5;
        int yOffset = size * -12;

        for (int x = 0; x < array.GetLength(0); x++)
        {
            for (int y = 0; y < array.GetLength(1); y++)
            {
                canvas.BrushColor = NumberToColor(array[x, y]);

                int tx = x * size;
                int ty = y * size;

                if (array[x, y] != 0)
                {
                    DrawCube(canvas, tx + positionX + xOffset, ty + positionY + yOffset);
                }

                if (drawBackground)
                {
                    if (y < 20 || array[x, y] != 0)
                    {
                        if (array[x, y] == 0)
                        {
                            if (!IsAnythingAbove(x, y, dynamicArray) || IsAnythingAbove(x, y, staticArray))
                            {
                                canvas.BrushColor = NumberToColor(currentShape + 1);
                                DrawCube(canvas, tx + xOffset, ty + yOffset);
                                canvas.BrushColor = backgroundColor;
                                DrawCube(canvas, tx + xOffset, ty + yOffset, 1);
                            }
                        }
                        else
                        {
                            DrawCube(canvas, tx + xOffset, ty + yOffset);
                        }
                    }
                }
            }
        }
    }


    private void DrawCube(Canvas canvas, int tx, int ty, int offset = 0)
    {
        for (int i = 0 + offset; i < size - offset; i++)
        {
            canvas.DrawLine(tx + i, ty + offset, tx + i, ty + size - offset);
        }
    }

    private void DrawNumber(Canvas canvas, int n, int xOffset = 0)
    {
        string s = numberFont[n];

        int index = 0;

        for (int y = 0; y < 5; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                if (s[index] == '1')
                {
                    DrawCube(canvas, x * size / 2 - (int)(Screen.Width / 2) + size * 2 * xOffset, y * size / 2 + (int)(Screen.Height / 2) - size * 3, size / 4);
                }
                index++;
            }
        }
    }


    /// <summary>
    /// Pelilogiikka pyörii täällä
    /// </summary>
    private void Update()
    {
        if (spawn)
        {
            SpawnNextShape();
            spawn = false;
        }

        if (spawnHeldBlock)
        {
            SpawnNextShape(nextShape, updateHold);
            nextShape = 0;
            updateHold = true;
            spawnHeldBlock = false;
            canHold = false;
        }

        MoveDown();
    }

    private bool MoveDown()
    {
        if (CanMoveWholeArray(dynamicArray, staticArray))
        {
            dynamicArray = MoveWholeArrayDown(dynamicArray);
        }
        else
        {
            staticArray = CombineArrays(staticArray, dynamicArray);
            CheckForFullLines();
            dynamicArray = Set2DArray(dynamicArray);
            CheckIfLost();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Piti tehdä omaksi loopiksensa että voi vaihtaa nopeutta
    /// </summary>
    private void FreefallLoop()
    {
        if (freefall)
        {
            bool stop = MoveDown();
            if (stop)
            {
                freefall = false;
            }
        }
    }

    /// <summary>
    /// Palauttaa oikean värin oikealle numerolle
    /// </summary>
    /// <param name="n">Numero</param>
    /// <returns>Numeroa vastaavan värin</returns>
    public static Color NumberToColor(int n)
    {
        Color[] colors = { Color.White, Color.Cyan, Color.Yellow, Color.Purple, Color.Green, Color.Blue, Color.Red, Color.Orange };

        return colors[n];
    }


    public static int[,] StringTo2DArray(string s, int size)
    {
        int[,] array = new int[size, size];

        for (int x = 0; x < array.GetLength(0); x++)
        {
            for (int y = 0; y < array.GetLength(1); y++)
            {
                array[x, y] = s[y * size + x] - 48; //array[x, y] = int.Parse(Char.ToString(s[y * size + x])); //Toimis kans mutta mitä turhia
            }
        }

        return array;
    }


    public static Vector FindStartPos(int[,] array)
    {
        return new Vector(FindStartX(array), FindStartY(array));
    }


    public static int FindStartY(int[,] array)
    {
        int startY = -1;

        for (int x = 0; x < array.GetLength(0); x++)
        {
            for (int y = 0; y < array.GetLength(1); y++)
            {
                if (array[x, y] != 0)
                {
                    startY = y;
                    return startY;
                }
            }
        }

        return startY;
    }


    public static int FindStartX(int[,] array)
    {
        int startX = -1;

        for (int y = 0; y < array.GetLength(1); y++)
        {
            for (int x = 0; x < array.GetLength(0); x++)
            {
                if (array[x, y] != 0)
                {
                    startX = x;
                    return startX;
                }
            }
        }

        return startX;
    }


    public static int[,] AddArrayToArrayAtPosition(int[,] small, int[,] big, int startX, int startY)
    {
        int[,] final = new int[big.GetLength(0), big.GetLength(1)];

        //Kopiodaan big -> final
        for (int x = 0; x < small.GetLength(0); x++)
        {
            for (int y = 0; y < small.GetLength(1); y++)
            {
                final[x, y] = big[x, y];
            }
        }

        //Vältetään index out of bounds virheet (a godsend)
        if (startX + small.GetLength(0) > big.GetLength(0))
        {
            startX = big.GetLength(0) - small.GetLength(0);
        }

        if (startY + small.GetLength(1) > big.GetLength(1))
        {
            startY = big.GetLength(1) - small.GetLength(1);
        }
        if (startX < 0)
        {
            startX = 0;
        }
        if (startY < 0)
        {
            startY = 0;
        }

        //Tehään se itse juttu
        for (int x = 0; x < small.GetLength(0); x++)
        {
            for (int y = 0; y < small.GetLength(1); y++)
            {
                final[startX + x, startY + y] = small[x, y];
            }
        }

        return final;
    }

    private void Restart()
    {
        lost = false;
        dynamicArray = Set2DArray(dynamicArray);
        staticArray = Set2DArray(staticArray);
        holdArray = Set2DArray(holdArray);
        heldShape = 0;
        score = 0;
        SpawnNextShape();
    }

    private void FreefallOn()
    {
        freefall = true;
    }

    private void FreefallOff()
    {
        freefall = false;
    }

    private void HoldUp()
    {
        if (canHold && !lost && heldShape != currentShape)
        {
            nextShape = 0;
            updateHold = true;

            if (heldShape == -1)
            {
                heldShape = currentShape;
                holdArray = AddArrayToArrayAtPosition(StringTo2DArray(shapeStrings[heldShape][0], shapeArraySize[heldShape]), holdArray, 0, 0);
            }
            else
            {
                nextShape = heldShape;
                heldShape = currentShape;
                holdArray = AddArrayToArrayAtPosition(StringTo2DArray(shapeStrings[heldShape][0], shapeArraySize[heldShape]), holdArray, 0, 0);
                currentShape = nextShape;
                updateHold = false;
            }

            dynamicArray = Set2DArray(dynamicArray);
            spawnHeldBlock = true;
            //SpawnNextShape(nextShape, updateHold);      //ajoitusongelma? tee spawn = true ja jotenkin nuo sinne messiin kans
            canHold = false;
        }
    }

    private void SlamDown()
    {
        if (FindStartX(dynamicArray) != -1) //pitää olla jotakin mitä liikuttaa alas tai jäädään jumiin
        {
            while (true)
            {
                bool stop = MoveDown();
                if (stop)
                {
                    break;
                }
            }
        }
    }

    private void Rotate()
    {
        if (IsArrayEmpty(dynamicArray) || CanRotate(dynamicArray, staticArray, currentRotation, currentShape, shapeStrings, shapeArraySize, shapeOffsets))
        {
            var result = RotateInArray(dynamicArray, currentRotation, currentShape, shapeStrings, shapeArraySize, shapeOffsets);
            dynamicArray = result.array;
            currentRotation = result.currentRotation;
        }
    }

    public static bool IsArrayEmpty(int[,] array)
    {
        for (int x = 0; x < array.GetLength(0); x++)
        {
            for (int y = 0; y < array.GetLength(0); y++)
            {
                if (array[x, y] != 0)
                {
                    return false;
                }
            }
        }
        return true;
    }

    //Luodaan uusi jossa pyöräytetään ja katsotaan meneekö pyöräytys päällekkäin aikaisempien palikoiden kanssa
    public static bool CanRotate(int[,] rotationArray, int[,] staticArray, int cRotation, int cShape, List<string[]> shapeStrings, int[] shapeArraySize, List<Vector[]> shapeOffsets)
    {
        int[,] newArray = new int[rotationArray.GetLength(0), rotationArray.GetLength(1)];

        for (int x = 0; x < newArray.GetLength(0); x++)
        {
            for (int y = 0; y < newArray.GetLength(0); y++)
            {
                newArray[x, y] = rotationArray[x, y];
            }
        }

        var result = RotateInArray(newArray, cRotation, cShape, shapeStrings, shapeArraySize, shapeOffsets);
        newArray = result.array;

        if (ArraysOverlap(newArray, staticArray))
        {
            return false;
        }

        return true;
    }

    //Piti saada aliohjelmasta palautettua kaksi arvoa
    public struct RotationResult
    {
        public int[,] array;
        public int currentRotation;
    }

    /// <summary>
    /// Oli alunperin private voidissa niin sentakia pitää olla tämä struct pelleily mukana.
    /// Nyt kun tehty tälläiseksi mistä tahansa kutsuttavaksi, on tosi helppo testata etukäteen
    /// kääntää palikkaa ja katsoa meneekö se päällekkäin jo jonkun olemassaolevan palikan kanssa.
    /// Toivonmukaan on myös yhteensopiva myöhemmin sen kanssa kun yritän estää palikoita "karkaamasta"
    /// niitä pyöritellessä
    /// </summary>
    /// <param name="rotationArray"></param>
    /// <param name="cRotation"></param>
    /// <param name="cShape"></param>
    /// <param name="shapeStrings"></param>
    /// <param name="shapeArraySize"></param>
    /// <returns></returns>
    public static RotationResult RotateInArray(int[,] rotationArray, int cRotation, int cShape, List<string[]> shapeStrings, int[] shapeArraySize, List<Vector[]> shapeOffsets)
    {
        Vector pos = FindStartPos(rotationArray);

        if (pos != new Vector(-1, -1))
        {
            pos += RotationOffset(cShape, cRotation, shapeOffsets);

            rotationArray = Set2DArray(rotationArray);
            cRotation++;
            if (cRotation > shapeStrings[cShape].Length - 1)
            {
                cRotation = 0;
            }
            rotationArray = AddArrayToArrayAtPosition(StringTo2DArray(shapeStrings[cShape][cRotation], shapeArraySize[cShape]), rotationArray, (int)pos.X, (int)pos.Y);

        }

        var result = new RotationResult
        {
            array = rotationArray,
            currentRotation = cRotation
        };

        return result;
    }

    public static Vector RotationOffset(int currentShape, int currentRotation, List<Vector[]> shapeOffsets)
    {
        return shapeOffsets[currentShape][currentRotation];
    }

    public static bool ArraysOverlap(int[,] a, int[,] b)
    {
        for (int x = 0; x < a.GetLength(0); x++)
        {
            for (int y = 0; y < a.GetLength(1); y++)
            {
                if (a[x, y] != 0)
                {
                    if (b[x, y] != 0)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    //TODO: Yhdistä MoveRight() tämän kanssa
    private void MoveLeft()
    {
        if (CanMoveHorizontally(staticArray, dynamicArray, 0, -1))
        {
            for (int x = 0; x < dynamicArray.GetLength(0); x++)
            {
                for (int y = 0; y < dynamicArray.GetLength(1); y++)
                {
                    if (x == dynamicArray.GetLength(0) - 1)
                    {
                        dynamicArray[x, y] = 0;
                    }
                    else
                    {
                        dynamicArray[x, y] = dynamicArray[x + 1, y];
                    }
                }
            }
        }
    }

    private void MoveRight()
    {
        if (CanMoveHorizontally(staticArray, dynamicArray, 9, 1))
        {
            for (int x = dynamicArray.GetLength(0) - 1; x >= 0; x--)
            {
                for (int y = 0; y < dynamicArray.GetLength(1); y++)
                {
                    if (x == 0)
                    {
                        dynamicArray[x, y] = 0;
                    }
                    else
                    {
                        dynamicArray[x, y] = dynamicArray[x - 1, y];
                    }
                }
            }
        }
    }

    public static bool CanMoveHorizontally(int[,] staticArray, int[,] dynamicArray, int vx, int direction)
    {
        //ei mennä reunojen yli
        for (int i = 0; i < dynamicArray.GetLength(1); i++)
        {
            if (dynamicArray[vx, i] != 0)
            {
                return false;
            }
        }

        for (int x = 0; x < dynamicArray.GetLength(0); x++)
        {
            for (int y = 0; y < dynamicArray.GetLength(1); y++)
            {
                if (dynamicArray[x, y] != 0)
                {
                    if (staticArray[x + direction, y] != 0)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    public static bool CanMoveWholeArray(int[,] dynamicArray, int[,] staticArray)
    {
        for (int x = 0; x < dynamicArray.GetLength(0); x++)
        {
            for (int y = 0; y < dynamicArray.GetLength(1); y++)
            {
                if (dynamicArray[x, y] != 0)
                {
                    if (y == 0 || staticArray[x, y - 1] != 0)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }


    public static int[,] MoveWholeArrayDown(int[,] dynamicArray)
    {
        int[,] newArray = new int[dynamicArray.GetLength(0), dynamicArray.GetLength(1)];

        for (int x = 0; x < dynamicArray.GetLength(0); x++)
        {
            for (int y = 1; y < dynamicArray.GetLength(1); y++)
            {
                newArray[x, y] = 0;
                if (dynamicArray[x, y] != 0)
                {
                    newArray[x, y - 1] = dynamicArray[x, y];
                }
            }
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
        for (int x = 0; x < a.GetLength(0); x++)
        {
            for (int y = 0; y < a.GetLength(1); y++)
            {
                if (a[x, y] != 0)
                {
                    c[x, y] = a[x, y];
                }
                if (b[x, y] != 0)
                {
                    c[x, y] = b[x, y];
                }
            }
        }

        return c;
    }


    private void CheckForFullLines()
    {
        List<int> linesToRemove = new List<int>();

        for (int y = 0; y < staticArray.GetLength(1); y++)
        {
            bool full = true;
            for (int x = 0; x < staticArray.GetLength(0); x++)
            {
                if (staticArray[x, y] == 0)
                {
                    full = false;
                }
            }
            if (full)
            {
                DestroyLine(y);
                score++; //sait pisteen

                linesToRemove.Add(y);
            }
        }

        for (int i = 0; i < linesToRemove.Count; i++)
        {
            MoveDownFromY(linesToRemove[i] - i);
        }
    }

    private void DestroyLine(int cy)
    {
        for (int x = 0; x < staticArray.GetLength(0); x++)
        {
            staticArray[x, cy] = 0;
        }
    }

    private void MoveDownFromY(int cy)
    {
        for (int x = 0; x < staticArray.GetLength(0); x++)
        {
            for (int y = cy; y < staticArray.GetLength(1) - 1; y++)
            {
                staticArray[x, y] = staticArray[x, y + 1];
            }
        }
    }

    private void CheckIfLost()
    {
        bool didLose = false;

        for (int i = 0; i < 10; i++)
        {
            if (staticArray[i, 20] != 0)
            {
                didLose = true;
            }
        }
        if (!didLose)
        {
            spawn = true;
            currentRotation = 0;
        }
        else
        {
            lost = true; //hävisit pelin
        }
    }

    public static int[,] Set2DArray(int[,] array, int toWhat = 0)
    {
        for (int x = 0; x < array.GetLength(0); x++)
        {
            for (int y = 0; y < array.GetLength(1); y++)
            {
                array[x, y] = toWhat;
            }
        }
        return array;
    }

    private void SpawnNextShape(int specificShape = 0, bool updateUpcomingArray = true)
    {
        if (specificShape != 0)
        {
            currentShape = specificShape;
        }

        if (updateUpcomingArray)
        {
            if (forcedShape == 0)
            {
                currentShape = upcomingShape;
                upcomingShape = RandomGen.NextInt(shapes.Length);
            }
            else
            {
                currentShape = forcedShape - 1;
                upcomingShape = forcedShape - 1;
            }

            upcomingArray = AddArrayToArrayAtPosition(StringTo2DArray(shapeStrings[upcomingShape][0], shapeArraySize[upcomingShape]), upcomingArray, 0, 0);
        }

        SpawnSpecificShape(shapes[currentShape]);
        canHold = true;
    }

    private void SpawnSpecificShape(string shape = "")
    {
        currentRotation = 0;
        for (int i = 0; i < shapes.Length; i++)
        {
            if (shape == shapes[i])
            {
                int startX = SpawnStartPos(shapeStartPositons[i]);
                int startY = SpawnStartPos(shapeStartPositons[i], false);
                dynamicArray = AddArrayToArrayAtPosition(StringTo2DArray(shapeStrings[i][0], shapeArraySize[i]), dynamicArray, startX, startY);
            }
        }
    }


    public static int SpawnStartPos(string s, bool firstNumber = true)
    {
        if (firstNumber)
        {
            return (int)s[0] - 48;
        }
        return (int)(s[1] - 48) * 10 + (s[2] - 48);
    }


    /// <summary>
    /// Zoomataan kamera oikealla lailla
    /// ja laitetaan tausta mustaksi
    /// </summary>
    private void SetupGame()
    {
        //Camera.Zoom(0.5);
        //Camera.X = 224;
        //Camera.Y = 477;
        Level.BackgroundColor = backgroundColor;

        if (forcedShape == 0)
        {
            upcomingShape = RandomGen.NextInt(shapes.Length);
        }
        else
        {
            upcomingShape = forcedShape;
        }
    }


    /// <summary>
    /// Tehään timeri updatelooppia varten
    /// </summary>
    private void SetupLoops()
    {
        Timer updateTimer = new Timer { Interval = 0.3 };
        updateTimer.Timeout += Update;
        updateTimer.Start();

        Timer freefallTimer = new Timer { Interval = 0.03 };
        freefallTimer.Timeout += FreefallLoop;
        freefallTimer.Start();
    }


    /// <summary>
    /// Alustetaan taulukot käyttökuntoon
    /// </summary>
    private void SetupArrays()
    {
        staticArray = new int[staticArray.GetLength(0), staticArray.GetLength(1)];
        dynamicArray = new int[staticArray.GetLength(0), staticArray.GetLength(1)];

        staticArray = Set2DArray(staticArray);
        dynamicArray = Set2DArray(dynamicArray);
        upcomingArray = Set2DArray(upcomingArray);
        holdArray = Set2DArray(holdArray);
    }

    public static bool IsAnythingAbove(int sx, int sy, int[,] array)
    {
        for (int y = sy; y < array.GetLength(1); y++)
        {
            if (array[sx, y] != 0)
            {
                return true;
            }
        }
        return false;
    }
}
