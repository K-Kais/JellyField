using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

public class JellyCombineGrid
{
    private JellyCell[,] grid = new JellyCell[2, 4];
    private List<JellyCell> results;

    public void InitializeLeftRight(List<JellyCell> jellyCells)
    {
        grid[0, 0] = jellyCells[0]; grid[0, 2] = jellyCells[4];
        grid[0, 1] = jellyCells[1]; grid[0, 3] = jellyCells[5];
        grid[1, 0] = jellyCells[2]; grid[1, 2] = jellyCells[6];
        grid[1, 1] = jellyCells[3]; grid[1, 3] = jellyCells[7];
    }
    public void InitializeTopDown(List<JellyCell> jellyCells)
    {
        grid[0, 0] = jellyCells[1]; grid[0, 2] = jellyCells[5];
        grid[0, 1] = jellyCells[3]; grid[0, 3] = jellyCells[7];
        grid[1, 0] = jellyCells[0]; grid[1, 2] = jellyCells[4];
        grid[1, 1] = jellyCells[2]; grid[1, 3] = jellyCells[6];
    }
    public List<JellyCell> GetResults()
    {
        if (!NextTo()) return null;

        results.AddRange(grid);
        if (IsUniformColor(results)) return results;

        var checks = new List<Func<bool>> { Square, LTopLeft, LTopRight, LDownLeft, LDownRight, HorizontalTop, HorizontalDown, NextTo };
        foreach (var check in checks) if (check()) return results;
        return null;
    }
    private bool IsUniformColor(List<JellyCell> cells) => cells.Select(cell => cell.color).Distinct().Count() == 1;
    private bool Square()
    {
        results = new List<JellyCell>
        {
           grid[0, 0], grid[0, 1], grid[0, 2], grid[0, 3],
           grid[1, 0], grid[1, 1]
        };
        if (IsUniformColor(results)) return true;

        results = new List<JellyCell>
        {
           grid[0, 0], grid[0, 1], 
           grid[1, 0], grid[1, 1], grid[1, 2], grid[1, 3],
        };
        if (IsUniformColor(results)) return true;

        results = new List<JellyCell>
        {
           grid[0, 0],grid[0, 1], grid[0, 2], grid[0, 3],
                                  grid[1, 2], grid[1, 3]
        };
        if (IsUniformColor(results)) return true;

        results = new List<JellyCell>
        {
                                   grid[0, 2], grid[0, 3],
            grid[1, 0],grid[1, 1], grid[1, 2], grid[1, 3]
        };
        if (IsUniformColor(results)) return true;

        results = new List<JellyCell>
        {
           grid[0, 0], grid[0, 1], grid[0, 2],
           grid[1, 0], grid[1, 1], grid[1, 2]
        };
        if (IsUniformColor(results)) return true;

        var temp = results[5];
        results.RemoveAt(5);
        if (IsUniformColor(results)) return true;

        results.Add(temp);
        results.RemoveAt(2);
        if (IsUniformColor(results)) return true;

        results = new List<JellyCell>
        {
          grid[0, 1], grid[0, 2], grid[0, 3],
          grid[1, 1], grid[1, 2], grid[1, 3]
        };
        if (IsUniformColor(results)) return true;

        temp = results[3];
        results.RemoveAt(3);
        if (IsUniformColor(results)) return true;

        results.Add(temp);
        results.RemoveAt(0);
        if (IsUniformColor(results)) return true;

        results = new List<JellyCell>
        {
            grid[0, 1], grid[0, 2],
            grid[1, 1], grid[1, 2]
        };
        if (IsUniformColor(results)) return true;
        return false;
    }
    private bool NextTo()
    {
        results = new List<JellyCell>
        {
            grid[0, 1], grid[0, 2],
        };
        if (IsUniformColor(results)) return true;

        results = new List<JellyCell>
        {
            grid[1, 1], grid[1, 2]
        };
        if (IsUniformColor(results)) return true;

        results.Clear();
        return false;
    }
    private bool HorizontalTop()
    {
        results = new List<JellyCell>
        {
            grid[0, 0], grid[0, 1], grid[0, 2], grid[0, 3]
        };
        if (IsUniformColor(results)) return true;

        var temp = results[3];
        results.RemoveAt(3);
        if (IsUniformColor(results)) return true;

        results.Add(temp);
        results.RemoveAt(0);
        if (IsUniformColor(results)) return true;
        return false;
    }
    private bool HorizontalDown()
    {
        results = new List<JellyCell>
        {
            grid[1, 0], grid[1, 1], grid[1, 2], grid[1, 3]
        };
        if (IsUniformColor(results)) return true;

        var temp = results[3];
        results.RemoveAt(3);
        if (IsUniformColor(results)) return true;

        results.Add(temp);
        results.RemoveAt(0);
        if (IsUniformColor(results)) return true;
        return false;
    }
    private bool LTopLeft()
    {
        results = new List<JellyCell>
        {
            grid[0, 1], grid[0, 2], grid[0, 3],
            grid[1, 1],
        };
        if (IsUniformColor(results)) return true;

        results.RemoveAt(2);
        if (IsUniformColor(results)) return true;
        return false;
    }
    private bool LTopRight()
    {
        results = new List<JellyCell>
        {
            grid[0, 0],grid[0, 1], grid[0, 2],
                                   grid[1, 2],
        };
        if (IsUniformColor(results)) return true;

        results.RemoveAt(0);
        if (IsUniformColor(results)) return true;
        return false;
    }
    private bool LDownLeft()
    {
        results = new List<JellyCell>
        {
            grid[0, 1],
            grid[1, 1], grid[1, 2], grid[1, 3]
        };
        if (IsUniformColor(results)) return true;

        results.RemoveAt(3);
        if (IsUniformColor(results)) return true;
        return false;
    }
    private bool LDownRight()
    {
        results = new List<JellyCell>
        {
                                   grid[0, 2],
            grid[1, 0],grid[1, 1], grid[1, 2],
        };
        if (IsUniformColor(results)) return true;

        results.RemoveAt(1);
        if (IsUniformColor(results)) return true;
        return false;
    }
}