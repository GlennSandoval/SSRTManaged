///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/**
 *	A method to compute a ray-AABB intersection.
 *	Original code by Andrew Woo, from "Graphics Gems", Academic Press, 1990
 *	Optimized code by Pierre Terdiman, 2000 (~20-30% faster on my Celeron 500)
 *	Epsilon value added by Klaus Hartmann. (discarding it saves a few cycles only)
 *
 *	Hence this version is faster as well as more robust than the original one.
 *
 *	Should work provided:
 *	1) the integer representation of 0.0f is 0x00000000
 *	2) the sign bit of the float is the most significant one
 *
 *	Report bugs: p.terdiman@codercorner.com
 *
 *	\param		aabb		[in] the axis-aligned bounding box
 *	\param		origin		[in] ray origin
 *	\param		dir			[in] ray direction
 *	\param		coord		[out] impact coordinates
 *	\return		true if ray intersects AABB
 */
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SSRTManaged {
	static class RayBox {
		//static ushort IR(double d){
		//    // Keeping this to match the cpp implementation.
		//    var arr = BitConverter.GetBytes(d);
		//    Array.Reverse(arr);
		//    return BitConverter.ToUInt16(arr, 0);
		//}

		const double RAYAABB_EPSILON = 0.00001d;
		public static bool RayAABB( double[] bounds, double[] origin, double[] dir, double[] coord ) {
			bool Inside = true;

			double[] MaxT = new double[3];
			double[] MinB = new double[3];
			double[] MaxB = new double[3];

			MinB[0] = bounds[0];
			MinB[1] = bounds[1];
			MinB[2] = bounds[2];
			MaxB[0] = bounds[3];
			MaxB[1] = bounds[4];
			MaxB[2] = bounds[5];

			MaxT[0] = MaxT[1] = MaxT[2] = -1.0d;

			// Find candidate planes.
			ushort i;
			for( i = 0; i < 3; i++ ) {
				if( origin[i] < MinB[i] ) {
					coord[i] = MinB[i];
					Inside = false;

					// Calculate T distances to candidate planes
					if( /*IR(dir[i])*/ dir[i] != 0 ) {
						MaxT[i] = ( MinB[i] - origin[i] ) / dir[i];
					}
				} else if( origin[i] > MaxB[i] ) {
					coord[i] = MaxB[i];
					Inside = false;

					// Calculate T distances to candidate planes
					if( /*IR(dir[i])*/ dir[i] != 0 ) {
						MaxT[i] = ( MaxB[i] - origin[i] ) / dir[i];
					}
				}
			}

			// Ray origin inside bounding box
			if( Inside ) {
				coord[0] = origin[0];
				coord[1] = origin[1];
				coord[2] = origin[2];
				return true;
			}

			// Get largest of the maxT's for final choice of intersection
			ushort WhichPlane = 0;
			if( MaxT[1] > MaxT[WhichPlane] )
				WhichPlane = 1;
			if( MaxT[2] > MaxT[WhichPlane] )
				WhichPlane = 2;

			// Check final candidate actually inside box
			//if( ( IR(MaxT[WhichPlane]) & 0x80000000 ) != 0 ) {
			//    return false;
			//}

			if( MaxT[WhichPlane] < 0 ) {
				return false;
			}


			for( i = 0; i < 3; i++ ) {
				if( i != WhichPlane ) {
					coord[i] = origin[i] + MaxT[WhichPlane] * dir[i];

					if( coord[i] < MinB[i] - RAYAABB_EPSILON || coord[i] > MaxB[i] + RAYAABB_EPSILON )
						return false;

				}
			}
			return true;	// ray hits box
		}

	}
}

