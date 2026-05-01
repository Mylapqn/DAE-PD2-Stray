using UnityEngine;

public static class LineSegmentMath
{
	public struct LinePoint
	{
		public int index;
		public Vector3 point;
		public float dist;
		public LinePoint(int index, Vector3 point, float dist)
		{
			this.index = index;
			this.point = point;
			this.dist = dist;
		}
	}
	public static LinePoint NearestLineRendererSegment(this LineRenderer line, Vector3 position)
	{
		Vector3[] linePositions = new Vector3[line.positionCount];
		line.GetPositions(linePositions);
		for (int i = 0; i < linePositions.Length; i++)
		{
			linePositions[i] = line.transform.TransformPoint(linePositions[i]);
		}
		return FindNearestLineSegment(linePositions, position);
	}
	static Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 p)
	{
		Vector3 ab = b - a;
		float t = Vector3.Dot(p - a, ab) / ab.sqrMagnitude;
		t = Mathf.Clamp01(t);
		return a + ab * t;
	}

	static LinePoint FindNearestLineSegment(Vector3[] points, Vector3 position)
	{
		float bestDist = float.MaxValue;
		int bestIndex = -1;
		Vector3 bestPoint = Vector3.zero;

		for (int i = 0; i < points.Length; i++)
		{
			Vector3 a = points[i];
			int nextIndex = (i + 1) % points.Length;
			Vector3 b = points[nextIndex];

			Vector3 closest = ClosestPointOnSegment(a, b, position);
			float squareDist = (position - closest).sqrMagnitude;

			if (squareDist < bestDist)
			{
				//bestDist = Mathf.Sqrt(squareDist);
				bestDist = squareDist;
				bestIndex = i;
				bestPoint = closest;
			}
		}

		return new LinePoint(bestIndex, bestPoint, Mathf.Sqrt(bestDist));
	}
}