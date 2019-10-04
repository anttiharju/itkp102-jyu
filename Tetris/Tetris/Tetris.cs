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

    //TODO: Jaetaan pelialue kolmeen eri taulukkoon:
    //dynamicArray = täällä on se yksi tippuva muodostelma ja sitä voi kieritellä tässä
    //staticArray = täällä ne jämähtäneet palikat ja poistetaan täydet rivit
    //drawArray = se mikä lähetetään tuonne paint hommelliin että ruudulla näkyy kaikki ohjen
    //              tähän vain siis yhdistetty nuo kaksi muuta taulukkoa

    public override void Begin()
    {
        SetupArrays();
        SetupLevel();
        SetupUpdateLoop();

        dynamicArray[0, 23] = 2;
        dynamicArray[1, 23] = 2;
        dynamicArray[2, 23] = 2;
        dynamicArray[3, 23] = 2;
        dynamicArray[4, 23] = 2;
        dynamicArray[5, 23] = 2;
        dynamicArray[6, 23] = 2;
        dynamicArray[7, 23] = 2;
        dynamicArray[8, 23] = 2;
        dynamicArray[9, 23] = 2;

        staticArray[5, 21] = 1;

        drawArray = CombineArrays(staticArray, dynamicArray);

        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
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


    /// <summary>
    /// Zoomataan kamera oikealla lailla
    /// ja laitetaan tausta mustaksi
    /// </summary>
    private void SetupLevel()
    {
        Camera.Zoom(0.5);
        Camera.X = 100;
        Camera.Y = 500;
        Level.BackgroundColor = Color.Black;
    }


    /// <summary>
    /// Tehään timeri updatelooppia varten
    /// </summary>
    private void SetupUpdateLoop()
    {
        Timer t = new Timer
        {
            Interval = 1.0
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
                        EmptyDynamicArray();
                        /*
                        staticArray[x, y] = dynamicArray[x, y];
                        dynamicArray[x, y] = 0;
                        */
                    }
                }
            }
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
                if(dynamicArray[x,y] != 0)
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
                dynamicArray[x,y] = 0;
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
    /// TODO: Älä piirrä neljään ylimpään riviin harmaita neliöitä koska jos staticArrayssä on siellä palikoita, peli loppuu
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

                canvas.DrawLine(tx - dSize, ty - dSize, tx - dSize, ty + dSize + 1); //+1 pakollinen ettei jää puuttumaan nurkata pikseliä
                canvas.DrawLine(tx + dSize, ty + dSize, tx + dSize, ty - dSize);
                canvas.DrawLine(tx - dSize, ty + dSize, tx + dSize, ty + dSize);
                canvas.DrawLine(tx + dSize, ty - dSize, tx - dSize, ty - dSize);
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


}
