using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;

public class Tetris : PhysicsGame
{
    public int forcedShape = 0;

    public double updateInterval = 0.3;
    public double freefallInterval = 0.05;

    public int size = 50;
    public int dSize = 22;

    public int[,] drawArray = new int[10, 24];
    public int[,] staticArray;
    public int[,] dynamicArray;

    public int[,] nextArray = new int[4, 4];

    public bool freefall = false;
    public bool spawn = false;
    public int currentRotation = 0;
    public int currentShape = 0;
    public int upcomingShape = 0;

    public int[] shapeArraySize = { 4, 2, 3, 3, 3, 3, 3 };
    public string[] shapeStartPositons = { "220", "421", "420", "321", "421", "421", "321" };
    public string[] shapes = { "stick", "block", "t", "worm", "corner", "wormR", "cornerR" };

    public string[] stick = { "0010001000100010", "0000000011110000" };
    public string[] block = { "2222" };
    public string[] t = { "000333030", "030033030", "030333000", "030330030" };
    public string[] worm = { "044440000", "400440040" };
    public string[] corner = { "000555005", "050050550", "500555000", "055050050" };
    public string[] wormR = { "660066000", "060660600" };
    public string[] cornerR = { "777700000", "700700770", "000007777", "770070070" };

    List<string[]> shapeStrings = new List<string[]>();

    public override void Begin()
    {
        shapeStrings = new List<string[]>() { stick, block, t, worm, corner, wormR, cornerR };

        SetupArrays();
        SetupLevel();
        SetupLoops();

        Keyboard.Listen(Key.D, ButtonState.Pressed, MoveRight, "Liiku oikealle");
        Keyboard.Listen(Key.A, ButtonState.Pressed, MoveLeft, "Liiku vasemmalle");
        Keyboard.Listen(Key.W, ButtonState.Pressed, Rotate, "Kieritä palikkaa");
        Keyboard.Listen(Key.S, ButtonState.Pressed, FreefallOn, "Nopeasti alas");
        Keyboard.Listen(Key.S, ButtonState.Released, FreefallOff, "");

        Keyboard.Listen(Key.Space, ButtonState.Pressed, SlamDown, "Iske alas");
        Keyboard.Listen(Key.R, ButtonState.Pressed, Restart, "Aloita alusta");

        if (forcedShape == 0)
        {
            upcomingShape = RandomGen.NextInt(shapes.Length);
        }
        else
        {
            upcomingShape = forcedShape;
        }

        SpawnRandomShape();

        PhoneBackButton.Listen(Exit, "Lopeta peli");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, Exit, "Lopeta peli");
    }


    /// <summary>
    /// Pelilogiikka pyörii täällä
    /// </summary>
    private void Update()
    {
        drawArray = CombineArrays(staticArray, dynamicArray);

        if (spawn)
        {
            SpawnRandomShape();
            spawn = false;
        }

        MoveDown();
        drawArray = CombineArrays(staticArray, dynamicArray);
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
            dynamicArray = SetArrayToZero(dynamicArray);
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
            drawArray = CombineArrays(staticArray, dynamicArray);
        }
    }

    /// <summary>
    /// Piirtää pelikentän
    /// </summary>
    protected override void Paint(Canvas canvas)
    {
        int[,] draw = drawArray; //visuaalista debuggausta varten

        for (int x = 0; x < draw.GetLength(0); x++)
        {
            for (int y = 0; y < draw.GetLength(1); y++)
            {
                canvas.BrushColor = NumberToColor(draw[x, y]);

                double tx = x * size;
                double ty = y * size;

                if (y < 20 || draw[x, y] != 0)
                {
                    if (draw[x, y] == 0)
                    {
                        if (!IsAnythingAbove(x, y, dynamicArray) || IsAnythingAbove(x, y, staticArray))
                        {
                            canvas.DrawLine(tx - dSize, ty - dSize, tx - dSize, ty + dSize + 1);
                            canvas.DrawLine(tx + dSize, ty + dSize, tx + dSize, ty - dSize);
                            canvas.DrawLine(tx - dSize, ty + dSize, tx + dSize, ty + dSize);
                            canvas.DrawLine(tx + dSize, ty - dSize, tx - dSize - 1, ty - dSize);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < size; i++)
                        {
                            canvas.DrawLine(tx - size / 2 + i, ty - size / 2, tx - size / 2 + i, ty + size / 2);
                        }
                    }
                }
            }
        }

        //Show next block

        int offsetX = size * (drawArray.GetLength(0) + 1);
        int offsetY = size * (drawArray.GetLength(1) - (nextArray.GetLength(1) * 2));

        for (int x = 0; x < nextArray.GetLength(0); x++)
        {
            for (int y = 0; y < nextArray.GetLength(1); y++)
            {
                canvas.BrushColor = NumberToColor(nextArray[x, y]);

                double tx = x * size;
                double ty = y * size;

                if (nextArray[x, y] != 0)
                {
                    for (int i = 0; i < size; i++)
                    {
                        canvas.DrawLine(offsetX + (tx - size / 2 + i), offsetY + (ty - size / 2), offsetX + (tx - size / 2 + i), offsetY + (ty + size / 2));
                    }
                }
            }
        }

        canvas.BrushColor = Color.DarkGray;
        canvas.DrawLine(-25, -25, dynamicArray.GetLength(0) * 50 - 25, -25);
        canvas.DrawLine(-25, -25, -25, (dynamicArray.GetLength(1) - 4) * 50 - 25);
        canvas.DrawLine(dynamicArray.GetLength(0) * 50 - 25, -25, dynamicArray.GetLength(0) * 50 - 25, (dynamicArray.GetLength(1) - 4) * 50 - 25);

        base.Paint(canvas);
    }


    /// <summary>
    /// Palauttaa oikean värin oikealle numerolle
    /// </summary>
    /// <param name="n">Numero</param>
    /// <returns>Numeroa vastaavan värin</returns>
    public static Color NumberToColor(int n)
    {
        Color[] colors = { Color.DarkGray, Color.Cyan, Color.Yellow, Color.Purple, Color.Green, Color.Blue, Color.Red, Color.Orange };

        return colors[n];
    }


    public static int[,] StringTo2DArray(string s, int size = 3)
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

        //Vältetään out of index virheet (a godsend)
        if (startX + small.GetLength(0) > big.GetLength(0))
        {
            startX = big.GetLength(0) - small.GetLength(0);
        }

        if (startY + small.GetLength(1) > big.GetLength(1))
        {
            startY = big.GetLength(1) - small.GetLength(1);
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
        dynamicArray = SetArrayToZero(dynamicArray);
        staticArray = SetArrayToZero(staticArray);
        SpawnRandomShape();
    }

    private void FreefallOn()
    {
        freefall = true;
    }

    private void FreefallOff()
    {
        freefall = false;
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
            drawArray = CombineArrays(staticArray, dynamicArray);
        }
    }

    private void Rotate()
    {
        if (CanRotate(dynamicArray, staticArray, currentRotation, currentShape, shapeStrings, shapeArraySize))
        {
            var result = Rotate(dynamicArray, currentRotation, currentShape, shapeStrings, shapeArraySize);
            dynamicArray = result.array;
            currentRotation = result.currentRotation;
            drawArray = CombineArrays(staticArray, dynamicArray);
        }
    }

    public static bool CanRotate(int[,] rotationArray, int[,] staticArray, int cRotation, int cShape, List<string[]> shapeStrings, int[] shapeArraySize)
    {
        int[,] newArray = new int[rotationArray.GetLength(0), rotationArray.GetLength(1)];

        for (int x = 0; x < newArray.GetLength(0); x++)
        {
            for (int y = 0; y < newArray.GetLength(0); y++)
            {
                newArray[x, y] = rotationArray[x, y];
            }
        }

        var result = Rotate(newArray, cRotation, cShape, shapeStrings, shapeArraySize);
        newArray = result.array;

        if (ArraysOverlap(newArray, staticArray))
        {
            return false;
        }

        return true;
    }

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
    public static RotationResult Rotate(int[,] rotationArray, int cRotation, int cShape, List<string[]> shapeStrings, int[] shapeArraySize)
    {
        Vector pos = FindStartPos(rotationArray);
        if (pos != new Vector(-1, -1))
        {
            rotationArray = SetArrayToZero(rotationArray);
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
        drawArray = CombineArrays(staticArray, dynamicArray);
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
        drawArray = CombineArrays(staticArray, dynamicArray);
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
                MessageDisplay.Add("sait pisteen!");

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
        bool lost = false;

        for (int i = 0; i < 10; i++)
        {
            if (staticArray[i, 20] != 0)
            {
                lost = true;
            }
        }
        if (!lost)
        {
            spawn = true;
            currentRotation = 0;
        }
        else
        {
            MessageDisplay.Add("Hävisit pelin!");
        }
    }

    public static int[,] SetArrayToZero(int[,] array)
    {
        for (int x = 0; x < array.GetLength(0); x++)
        {
            for (int y = 0; y < array.GetLength(1); y++)
            {
                array[x, y] = 0;
            }
        }
        return array;
    }

    private void SpawnRandomShape()
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
        ShowNextBlock();

        SpawnSpecificShape(shapes[currentShape]);
    }

    private void ShowNextBlock()
    {
        for (int x = 0; x < nextArray.GetLength(0); x++)
        {
            for (int y = 0; y < nextArray.GetLength(1); y++)
            {
                nextArray[x, y] = upcomingShape + 1;
            }
        }
        //nextArray = AddArrayToArrayAtPosition(StringTo2DArray(shapeStrings[upcomingShape][0], shapeArraySize[upcomingShape]), dynamicArray, 0, 0);
    }

    private void SpawnSpecificShape(string shape = "")
    {
        for (int i = 0; i < shapes.Length; i++)
        {
            if (shape == shapes[i])
            {
                int startX = SpawnStartPos(shapeStartPositons[i]);
                int startY = SpawnStartPos(shapeStartPositons[i], false);
                dynamicArray = AddArrayToArrayAtPosition(StringTo2DArray(shapeStrings[i][0], shapeArraySize[i]), dynamicArray, startX, startY);
            }
        }
        drawArray = CombineArrays(staticArray, dynamicArray);
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
    private void SetupLevel()
    {
        Camera.Zoom(0.5);
        Camera.X = 224;
        Camera.Y = 477;
        Level.BackgroundColor = Color.Black;
    }


    /// <summary>
    /// Tehään timeri updatelooppia varten
    /// </summary>
    private void SetupLoops()
    {
        Timer updateTimer = new Timer
        {
            Interval = updateInterval
        };
        updateTimer.Timeout += Update;
        updateTimer.Start();

        Timer freefallTimer = new Timer
        {
            Interval = freefallInterval
        };
        freefallTimer.Timeout += FreefallLoop;
        freefallTimer.Start();
    }


    /// <summary>
    /// Alustetaan taulukot käyttökuntoon
    /// </summary>
    private void SetupArrays()
    {
        staticArray = new int[drawArray.GetLength(0), drawArray.GetLength(1)];
        dynamicArray = new int[drawArray.GetLength(0), drawArray.GetLength(1)];

        for (int x = 0; x < drawArray.GetLength(0); x++)
        {
            for (int y = 0; y < drawArray.GetLength(1); y++)
            {
                drawArray[x, y] = 0;
                staticArray[x, y] = 0;
                dynamicArray[x, y] = 0;
            }
        }

        for (int x = 0; x < nextArray.GetLength(0); x++)
        {
            for (int y = 0; y < nextArray.GetLength(0); y++)
            {
                nextArray[x, y] = 0;
            }
        }
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
