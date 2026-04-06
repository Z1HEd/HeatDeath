using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

public interface IHitter
{
    public float Damage { get; set; }
    public float KnockbackPower { get; set; }
    public Vector3 GetPosition();

}
