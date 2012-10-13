/********************************************************************************************
 ** SSRT - Shaun's Simple Ray Tracer                                                       **
 **        By Shaun Nirenstein: shaun@cs.uct.ac.za                                         **
 **                             http://people.cs.uct.ac.za/~snirenst                       **
 **                                                                                        **
 ** License agreement:                                                                     **
 ** - This source code (or compiled version thereof) CANNOT be used freely for commercial  **
 **   purposes.  If you wish to use it for such purposes, please contact the author,       **
 **   Shaun Nirenstein (shaun@cs.uct.ac.za).                                               **
 ** - For educational purposes (personal or institutional), this source code may be        **
 **   modified, distributed or extended freely, as long as this license agreement appears  **
 **   at the top of every file as is.                                                      **
 ** - Any program modified or extended from a version of SSRT (or a modified or extended   **
 **   version thereof) must acknowledge that it is SSRT derived in an accessible about box **
 **   or credit screen.                                                                    **
 **                                                                                        **
 ********************************************************************************************
 */
using System;

namespace SSRTManaged {
	static class Utils {

		static Random RAND01 = new Random();

		public static void setf3( double[] target, double[] source ) {
			for(int i = 0; i < source.Length; i++){
				target[i] = source[i];
			}
		}

		public static void sub3f( double[] target, double[] source1, double[] source2 ) {
			target[0] = source1[0] - source2[0];
			target[1] = source1[1] - source2[1];
			target[2] = source1[2] - source2[2];
		}

		public static void neg3f( double[] target, double[] source ) {
			target[0] = -source[0];
			target[1] = -source[1];
			target[2] = -source[2];
		}

		public static void neg3f( double[] target ) {
			target[0] = -target[0];
			target[1] = -target[1];
			target[2] = -target[2];
		}

		public static void sub3f( double[] target, double[] source ) {
			target[0] -= source[0];
			target[1] -= source[1];
			target[2] -= source[2];
		}

		public static void add3f( double[] target, double[] source1, double[] source2 ) {
			target[0] = source1[0] + source2[0];
			target[1] = source1[1] + source2[1];
			target[2] = source1[2] + source2[2];
		}

		public static void add3f( double[] target, double[] source ) {
			target[0] += source[0];
			target[1] += source[1];
			target[2] += source[2];
		}

		public static void mul3f( double[] target, double[] source1, double[] source2 ) {
			target[0] = source1[0] * source2[0];
			target[1] = source1[1] * source2[1];
			target[2] = source1[2] * source2[2];
		}

		public static void mul3f( double[] target, double[] source ) {
			target[0] *= source[0];
			target[1] *= source[1];
			target[2] *= source[2];
		}

		public static void normalise( double[] vector ) {
			double sum;
			sum = vector[0] * vector[0];
			sum += vector[1] * vector[1];
			sum += vector[2] * vector[2];

			sum = 1.0f / Math.Sqrt( sum );
			vector[0] *= sum;
			vector[1] *= sum;
			vector[2] *= sum;
		}

		public static double dot( double[] a, double[] b ) {
			return a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
		}

		/** 
		* Return the cosine of the angle between the two vectors.
		* No assumption of normalisation is made.
		*/
		public static double angle( double[] a, double[] b ) {
			double aLen = Math.Sqrt( a[0] * a[0] + a[1] * a[1] + a[2] * a[2] );
			double bLen = Math.Sqrt( b[0] * b[0] + b[1] * b[1] + b[2] * b[2] );
			return ( a[0] * b[0] + a[1] * b[1] + a[2] * b[2] ) / ( aLen * bLen );
		}

		public static void cross( double[] target, double[] p1, double[] p2 ) {
			target[0] = ( p1[1] * p2[2] ) - ( p1[2] * p2[1] );
			target[1] = ( p1[2] * p2[0] ) - ( p1[0] * p2[2] );
			target[2] = ( p1[0] * p2[1] ) - ( p1[1] * p2[0] );
		}

		public static void scale( double[] target, double sf ) {
			target[0] *= sf;
			target[1] *= sf;
			target[2] *= sf;
		}

		public static void scale( double[] target, double[] source, double sf ) {
			target[0] = source[0] * sf;
			target[1] = source[1] * sf;
			target[2] = source[2] * sf;
		}

		public static double length( double[] source ) {
			return Math.Sqrt( source[0] * source[0] + source[1] * source[1] + source[2] * source[2] );
		}

		public static void triangleNormal( double[][] tri, double[] normal ) {
			double[] v1 = new double[3];
			double[] v2 = new double[3];
			sub3f( v1, tri[1], tri[0] );
			sub3f( v2, tri[2], tri[0] );
			cross( normal, v1, v2 );
		}

		public static double triangleArea( double[][] tri ) {
			double[] normal = new double[3];
			triangleNormal( tri, normal );
			return length( normal ) / 2.0f;
		}

		public static void generatePointOnTriangle( double[][] tri, double[] point ) {
			double[] v1 = new double[3];
			double[] v2 = new double[3];
			sub3f( v1, tri[1], tri[0] );
			sub3f( v2, tri[2], tri[0] );
			double u = RAND01.NextDouble(), v = RAND01.NextDouble();
			if( u + v > 1 ) {
				u = 1.0f - v;
				v = 1.0f - u;
			}
			scale( v1, u );
			scale( v2, v );
			add3f( point, tri[0], v1 );
			add3f( point, v2 );
		}

		public static double distance( double[] a, double[] b ) {
			double[] vec = new double[3];
			sub3f( vec, a, b );
			return Math.Sqrt( dot( vec, vec ) );
		}

		public static double maximum3( double a, double b, double c ) {
			if( a > b )
				return a > c ? a : c;
			else
				return b > c ? b : c;
		}

		public static void reflect( double[] l, double[] n, double[] r ) { // l = light, n = normal, r = reflection vector
			double nl = -2 * dot( n, l );
			r[0] = nl * n[0] + l[0];
			r[1] = nl * n[1] + l[1];
			r[2] = nl * n[2] + l[2];
		}

		public static void refract( double[] l, double[] n, double sourceIOR, double targetIOR, double[] t ) { // l = light, n = normal, sourceIOR = source medium index of refraction, targetIOR = target medium index of refraction, r = reflection vector		
			double cos_i = dot( n, l );
			double b;
			if( cos_i >= 0.0 )
				b = sourceIOR / targetIOR;
			else
				b = targetIOR / sourceIOR;
			double cos_r = 1.0f - ( b * b ) * ( 1.0f - cos_i * cos_i );

			if( cos_r >= 0.0f ) {
				double a;
				if( cos_i >= 0.0f )
					a = b * cos_i - Math.Sqrt( cos_r );
				else
					a = b * cos_i + Math.Sqrt( cos_r );
				t[0] = a * n[0] - b * l[0];
				t[1] = a * n[1] - b * l[1];
				t[2] = a * n[2] - b * l[2];
			} else {
				scale( t, n, cos_i * 2 );
				sub3f( t, t, l );
			}
		}

	}
}

