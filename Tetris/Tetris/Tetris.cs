using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;

public class Tetris : PhysicsGame
{
    public int size = 50;
    public int dSize = 22;

    public int[,] drawArray = new int[10, 24];
    public int[,] staticArray;
    public int[,] dynamicArray;

    public bool spawn = false;
    public int currentRotation = 0;
    public int currentShape = 0;

    public int forcedShape = 0;

    public int[] shapeArraySize = { 3, 3, 4, 2, 3, 3, 3 };
    public string[] shapeStartPositons = { "421", "321", "220", "421", "321", "421", "420" };
    public string[] shapes = { "corner", "cornerR", "stick", "block", "worm", "wormR", "t" };

    public string[] corner = { "000555005", "050050550", "500555000", "055050050" };
    public string[] cornerR = { "777700000", "700700770", "000007777", "770070070" };
    public string[] stick = { "0010001000100010", "0000000011110000" };
    public string[] block = { "2222" };
    public string[] worm = { "044440000", "400440040" };
    public string[] wormR = { "660066000", "060660600" };
    public string[] t = { "000333030", "030033030", "030333000", "030330030" };


    List<string[]> shapeStrings = new List<string[]>();

    public override void Begin()
    {
        shapeStrings = new List<string[]>() { corner, cornerR, stick, block, worm, wormR, t };

        SetupArrays();
        SetupLevel();
        SetupUpdateLoop();

        Keyboard.Listen(Key.D, ButtonState.Pressed, MoveRight, "Liiku oikealle");
        Keyboard.Listen(Key.A, ButtonState.Pressed, MoveLeft, "Liiku vasemmalle");
        Keyboard.Listen(Key.W, ButtonState.Pressed, Rotate, "Kieritä palikkaa");
        Keyboard.Listen(Key.S, ButtonState.Down, FastDown, "Nopeasti alas");
        Keyboard.Listen(Key.Space, ButtonState.Pressed, SlamDown, "Iske alas");

        SpawnRandomShape();
        drawArray = CombineArrays(staticArray, dynamicArray);

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

        if (CanMoveWholeArray(dynamicArray, staticArray))
        {
            dynamicArray = MoveWholeArrayDown(dynamicArray);
        }
        else
        {
            staticArray = CombineArrays(staticArray, dynamicArray);
            CheckForFullLines();
            EmptyDynamicArray();
            CheckIfLost();
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

    private void FastDown()
    {
        if (CanMoveWholeArray(dynamicArray, staticArray))
        {
            dynamicArray = MoveWholeArrayDown(dynamicArray);
        }
        else
        {
            staticArray = CombineArrays(staticArray, dynamicArray);
            CheckForFullLines();
            EmptyDynamicArray();
            CheckIfLost();
        }
        drawArray = CombineArrays(staticArray, dynamicArray);
    }

    private void SlamDown()
    {
        while (true)
        {
            if (CanMoveWholeArray(dynamicArray, staticArray))
            {
                dynamicArray = MoveWholeArrayDown(dynamicArray);
            }
            else
            {
                staticArray = CombineArrays(staticArray, dynamicArray);
                CheckForFullLines();
                EmptyDynamicArray();
                CheckIfLost();
                break;
            }
        }
        drawArray = CombineArrays(staticArray, dynamicArray);
    }

    private void Rotate()
    {
        Vector pos = FindStartPos(dynamicArray);
        if (pos != new Vector(-1, -1))
        {
            EmptyDynamicArray();
            currentRotation++;
            if (currentRotation > shapeStrings[currentShape].Length - 1)
            {
                currentRotation = 0;
            }
            dynamicArray = AddArrayToArrayAtPosition(StringTo2DArray(shapeStrings[currentShape][currentRotation], shapeArraySize[currentShape]), dynamicArray, (int)pos.X, (int)pos.Y);

            drawArray = CombineArrays(staticArray, dynamicArray);
        }
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


    /// <summary>
    /// Tehdään dynaamisesta taulukosta tyhjä
    /// </summary>
    private void EmptyDynamicArray()
    {
        for (int x = 0; x < dynamicArray.GetLength(0); x++)
        {
            for (int y = 0; y < dynamicArray.GetLength(1); y++)
            {
                dynamicArray[x, y] = 0;
            }
        }
    }

    private void SpawnRandomShape()
    {
        if (forcedShape == 0)
        {
            currentShape = RandomGen.NextInt(shapes.Length);
        }
        else
        {
            currentShape = forcedShape - 1;
        }
        string nextShape = shapes[currentShape];
        SpawnSpecificShape(nextShape);
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
    private void SetupUpdateLoop()
    {
        Timer t = new Timer
        {
            Interval = 0.3
        };
        t.Timeout += Update;
        t.Start();
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
