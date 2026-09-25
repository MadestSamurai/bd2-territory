using UnityEngine;
namespace UnityEngine {public struct Vector2Int {public int x,y;public Vector2Int(int x,int y){this.x=x;this.y=y;}}}
namespace FieldEvent.Life.Chunk
{
 public enum CellKind {None,Ground,Water}
 public struct CellInfo {public bool isValid;public int chunkId,localX,localY;}
 public class GroundChunk:Component
 {public const float CELL_SIZE=.65f;public Func<int,int,CellKind> Read=(x,z)=>CellKind.Ground;public CellKind GetCellType(int x,int z)=>Read(x,z);}
 public class GroundChunkManager:Component
 {
  public GroundChunk Chunk=new();public bool Ready=true;
  public CellInfo GetCellAtWorldCell(int x,int z)=>new(){isValid=Ready&&x>=0&&z>=0&&x<50&&z<50,chunkId=1,localX=x,localY=z};
  public GroundChunk FindChunkById(int id)=>Ready?Chunk:null;
  public Vector3 WorldCellToWorldPosition(int x,int z)=>transform.TransformPoint(new((x-25)*GroundChunk.CELL_SIZE,0,(z-25)*GroundChunk.CELL_SIZE));
 }
 public class LifePlaceableObject:Component {public bool Water=true,Temporary;public object Db=new();public bool HasWaterCell()=>Water;}
 public class LifePlaceableObject_ColliderGate:Component {public bool isActiveAndEnabled=true;public BoxCollider gateCollider=new(){isTrigger=true};}
}
namespace FieldEvent.Life
{
 using Chunk;
 public class LifeChunkController
 {
  public GroundChunkManager Ground=new();public GroundChunkManager GetChunkManager()=>Ground;
  public Vector2Int WorldPositionToWorldCell(Vector3 p){p=Ground.transform.InverseTransformPoint(p);return new((int)MathF.Floor(p.x/GroundChunk.CELL_SIZE+25),(int)MathF.Floor(p.z/GroundChunk.CELL_SIZE+25));}
 }
}
