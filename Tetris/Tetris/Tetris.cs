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

    string[] shapes = { "long", "block", "t", "worm", "corner", "wormR", "cornerR" };

    public override void Begin()
    {
        SetupArrays();
        SetupLevel();
        SetupUpdateLoop();

        Keyboard.Listen(Key.Right, ButtonState.Pressed, MoveRight, "Liiku oikealle");
        Keyboard.Listen(Key.Left, ButtonState.Pressed, MoveLeft, "Liiku vasemmalle");

        SpawnRandomShape();
        drawArray = CombineArrays(staticArray, dynamicArray);

        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
    }

    private void MoveLeft()
    {
        if (CanMove(staticArray, dynamicArray, 0, -1))
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
        if (CanMove(staticArray, dynamicArray, 9, 1))
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

    public static bool CanMove(int[,] staticArray, int[,] dynamicArray, int vx, int direction)
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

    private void SpawnRandomShape()
    {
        string nextShape = shapes[RandomGen.NextInt(shapes.Length)];
        SpawnShape(nextShape);
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


    public static int[,] AddArrayToArray(int[,] bigArray, int[,] smallerArray)
    {
        int[,] tmp = new int[bigArray.GetLength(0), bigArray.GetLength(1)];

        for (int x = 0; x < smallerArray.GetLength(0); x++)
        {
            for (int y = 0; y < smallerArray.GetLength(1); y++)
            {
                tmp[x, y] = smallerArray[x, y];
            }
        }

        return tmp;
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
            Interval = 1
        };
        t.Timeout += UpdateLoop;
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


    /// <summary>
    /// Täällä pelilogiikka kai pyörii
    /// Jypelin kai pitäisi olla
    /// event pohjanen tjsp. mutta ¯\_(ツ)_/¯
    /// </summary>
    private void UpdateLoop()
    {
        drawArray = CombineArrays(staticArray, dynamicArray);

        if (spawn)
        {
            SpawnRandomShape();
            spawn = false;
        }

        for (int x = 0; x < dynamicArray.GetLength(0); x++)
        {
            for (int y = 0; y < dynamicArray.GetLength(1); y++)
            {
                if (dynamicArray[x, y] != 0)
                {
                    if (CanMoveDown(staticArray, x, y))
                    {
                        dynamicArray = MoveDown(dynamicArray, x, y);
                    }
                    else
                    {
                        ReverseMoveUp(x, y);
                        staticArray = CombineArrays(staticArray, dynamicArray);
                        CheckForFullLines();
                        EmptyDynamicArray();
                        CheckIfLost();
                    }
                }
            }
        }
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
        }
        else
        {
            MessageDisplay.Add("Hävisit pelin!");
        }
    }


    /// <summary>
    /// Helpompi siirtää jo siirretyt palikat takas ylöspäin
    /// kuin ensin katsoa voiko kaikkia palikoita siirtää,
    /// tallentaa se johonkin ja sitten siirtää ne kaikki
    /// </summary>
    /// <param name="cx">missä kohtaa x oltiin menossa</param>
    /// <param name="cy">missä kohtaa y oltiin menossa</param>
    private void ReverseMoveUp(int cx, int cy)
    {
        cx--;
        for (int x = cx; x >= 0; x--)
        {
            for (int y = cy; y >= 0; y--)
            {
                if (dynamicArray[x, y] != 0)
                {
                    dynamicArray[x, y + 1] = dynamicArray[x, y];
                    dynamicArray[x, y] = 0;
                }
            }
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


    /// <summary>
    /// Liikuttaa taulukossa olevaa lukua
    /// y-akselilla alaspäin yhdellä
    /// </summary>
    /// <param name="array"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static int[,] MoveDown(int[,] array, int x, int y)
    {
        array[x, y - 1] = array[x, y];
        array[x, y] = 0;
        return array;
    }


    /// <summary>
    /// Tarkistaa voiko kyseisessä koordinaatissa
    /// olevan jutun siirtää yhtä alemmaksi
    /// </summary>
    /// <param name="array">taulukko</param>
    /// <param name="x">d'oh</param>
    /// <param name="y">d'oh</param>
    /// <returns>voiko vai eikö</returns>
    public static bool CanMoveDown(int[,] array, int x, int y)
    {
        if (y > 0)
        {
            if (array[x, y - 1] == 0)
            {
                return true;
            }
        }
        return false;
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
                    canvas.DrawLine(tx - dSize, ty - dSize, tx - dSize, ty + dSize + 1);
                    canvas.DrawLine(tx + dSize, ty + dSize, tx + dSize, ty - dSize);
                    canvas.DrawLine(tx - dSize, ty + dSize, tx + dSize, ty + dSize);
                    canvas.DrawLine(tx + dSize, ty - dSize, tx - dSize - 1, ty - dSize);
                }
            }
        }
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


    //TODO: Joku aliohjelma ettei vie näin paljoa tilaa
    private void SpawnShape(string shape = "")
    {
        if (shape == "long")
        {
            dynamicArray[5, 23] = 1;
            dynamicArray[5, 22] = 1;
            dynamicArray[5, 21] = 1;
            dynamicArray[5, 20] = 1;
        }

        if (shape == "block")
        {
            dynamicArray[4, 22] = 2;
            dynamicArray[5, 22] = 2;
            dynamicArray[4, 21] = 2;
            dynamicArray[5, 21] = 2;
        }

        if (shape == "t")
        {
            dynamicArray[4, 22] = 3;
            dynamicArray[5, 22] = 3;
            dynamicArray[4, 21] = 3;
            dynamicArray[4, 23] = 3;
        }

        if (shape == "worm")
        {
            dynamicArray[5, 22] = 4;
            dynamicArray[6, 22] = 4;
            dynamicArray[4, 21] = 4;
            dynamicArray[5, 21] = 4;
        }

        if (shape == "corner")
        {
            dynamicArray[3, 22] = 5;
            dynamicArray[3, 21] = 5;
            dynamicArray[4, 21] = 5;
            dynamicArray[5, 21] = 5;
        }

        if (shape == "wormR")
        {
            dynamicArray[3, 22] = 6;
            dynamicArray[4, 22] = 6;
            dynamicArray[5, 21] = 6;
            dynamicArray[4, 21] = 6;
        }

        if (shape == "cornerR")
        {
            dynamicArray[4, 21] = 7;
            dynamicArray[5, 21] = 7;
            dynamicArray[6, 21] = 7;
            dynamicArray[6, 22] = 7;
        }
    }


}
