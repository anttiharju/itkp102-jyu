using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;

public class Tetris : PhysicsGame
{
    int size = 50;
    int dSize = 22;

    int[,] playArea = new int[10, 24];

    //TODO: Jaetaan pelialue kolmeen eri taulukkoon:
    //dynamicTable = täällä on se yksi tippuva muodostelma ja sitä voi kieritellä tässä
    //staticTable = täällä ne jämähtäneet palikat ja poistetaan täydet rivit
    //drawTable = se mikä lähetetään tuonne paint hommelliin että ruudulla näkyy kaikki ohjen
    //              tähän vain siis yhdistetty nuo kaksi muuta taulukkoa

    public override void Begin()
    {

        for (int x = 0; x < playArea.GetLength(0); x++)
        {
            for (int y = 0; y < playArea.GetLength(1); y++)
            {
                playArea[x, y] = 0;
            }
        }

        playArea[9, 23] = 1;
        playArea[8, 23] = 2;
        playArea[7, 23] = 3;
        playArea[6, 23] = 4;
        playArea[5, 23] = 5;
        playArea[4, 23] = 6;
        playArea[3, 23] = 7;
        playArea[2, 23] = 6;
        playArea[1, 23] = 5;
        playArea[0, 23] = 4;


        Camera.Zoom(0.5);
        Camera.X = 100;
        Camera.Y = 500;
        Level.BackgroundColor = Color.Black;
        Timer t = new Timer();
        t.Interval = 1.0;
        t.Timeout += UpdateLoop;
        t.Start();

        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
    }


    /// <summary>
    /// Täällä pelilogiikka kai pyörii
    /// Jypelin kai pitäisi olla
    /// event pohjanen tjsp. mutta ¯\_(ツ)_/¯
    /// </summary>
    private void UpdateLoop()
    {
        UpdateTable(playArea);
    }


    /// <summary>
    /// Hoidetaan taulukkohommat erillään
    /// </summary>
    /// <param name="playArea"></param>
    public static void UpdateTable(int[,] playArea)
    {
        for (int x = 0; x < playArea.GetLength(0); x++)
        {
            for (int y = 0; y < playArea.GetLength(1); y++)
            {
                if (CanMoveDown(playArea, x, y))
                {
                    playArea = MoveDown(playArea, x, y);
                }
            }
        }
    }


    /// <summary>
    /// Liikuttaa taulukossa olevaa lukua
    /// y-akselilla alaspäin yhdellä
    /// </summary>
    /// <param name="playArea"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static int[,] MoveDown(int[,] playArea, int x, int y)
    {
        playArea[x, y - 1] = playArea[x, y];
        playArea[x, y] = 0;
        return playArea;
    }


    /// <summary>
    /// Tarkistaa voiko kyseisessä koordinaatissa
    /// olevan jutun siirtää yhtä alemmaksi
    /// </summary>
    /// <param name="playArea">taulukko</param>
    /// <param name="x">d'oh</param>
    /// <param name="y">d'oh</param>
    /// <returns>voiko vai eikö</returns>
    public static bool CanMoveDown(int[,] playArea, int x, int y)
    {
        if(y > 0)
        {
            if(playArea[x,y-1] == 0)
            {
                return true;
            }
        }
        return false;
    }


    /// <summary>
    /// Piirtää pelikentän
    /// TODO: Käytä canvas.DrawImage();
    /// TODO: Piirrä tyhjät ruudut harmaalla yms
    /// </summary>
    protected override void Paint(Canvas canvas)
    {
        for (int x = 0; x < playArea.GetLength(0); x++)
        {
            for (int y = 0; y < playArea.GetLength(1); y++)
            {
                if (playArea[x, y] != 0)
                {
                    canvas.BrushColor = NumberToColor(playArea[x, y]);

                    double tx = x * size;
                    double ty = y * size;

                    canvas.DrawLine(tx - dSize, ty - dSize, tx - dSize, ty + dSize);
                    canvas.DrawLine(tx + dSize, ty + dSize, tx + dSize, ty - dSize);

                    canvas.DrawLine(tx - dSize, ty + dSize, tx + dSize, ty + dSize);
                    canvas.DrawLine(tx + dSize, ty - dSize, tx - dSize, ty - dSize);
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
        Color[] colors = { Color.Cyan, Color.Yellow, Color.Purple, Color.Green, Color.Blue, Color.Red, Color.Orange };

        return colors[n - 1];
    }


}
