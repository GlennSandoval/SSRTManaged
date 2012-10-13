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
using System.Collections.Generic;
namespace SSRTManaged {

	class PointLight {

		public double[] m_point = new double[3];
		public double[] m_plane = new double[4]; ///< Plane in which the light source lies (i.e., the plane of the area lightsource from which it was computed)
		public int m_objectIndex = -1; ///< Pointer back to object containing light source
		public int m_triangleIndex = -1; ///< Pointer back to triangle containing light source
	};

	class RayTracer {

		private Random rand = new Random();

		public RayTracer( Model model, byte[][] bitmap ) {
			m_model = model;
			m_bitmap = bitmap;
			for( int y = 0; y < m_model.m_height; y++ )
				for( int x = 0; x < m_model.m_width; x++ ) {
					m_bitmap[x + y * m_model.m_width] = new byte[3];
					m_bitmap[x + y * m_model.m_width][0] = (byte)( ( x * 5 + y * 7 ) % 256 ); // B
					m_bitmap[x + y * m_model.m_width][1] = (byte)( ( x * 3 + y * 2 ) % 256 ); // G
					m_bitmap[x + y * m_model.m_width][2] = (byte)( ( x + y ) % 256 ); // R
				}
			m_pixels = new double[m_model.m_width * m_model.m_height][];

			for( int i = 0; i < m_pixels.Length; i++ ) {
				m_pixels[i] = new double[3];
			}

			m_currentLine = 0;
			m_tracing = true;

			// Build camera vectors	
			double aspectRatio = (double)m_model.m_width / (double)m_model.m_height;
			Utils.cross( m_uVec, m_model.m_direction, m_model.m_up );
			Utils.cross( m_vVec, m_model.m_direction, m_uVec );
			Utils.normalise( m_uVec );
			Utils.normalise( m_vVec );
			Utils.scale( m_uVec, aspectRatio );
			m_pixelSize[0] = aspectRatio / (double)m_model.m_width;
			m_pixelSize[1] = 1.0d / (double)m_model.m_width;

			// Corner
			double[] direction = new double[3];
			double[] halfU = new double[3];
			double[] halfV = new double[3];

			Utils.setf3( direction, m_model.m_direction );
			Utils.normalise( direction );
			Utils.setf3( halfU, m_uVec );
			Utils.scale( halfU, -0.5d );
			Utils.setf3( halfV, m_vVec );
			Utils.scale( halfV, -0.5d );
			Utils.add3f( m_corner, m_model.m_eye, direction ); // Corner is now in the center of the image plane
			Utils.add3f( m_corner, halfU ); // Now Corner is on the left border in the center
			Utils.add3f( m_corner, halfV ); // Now Corner is on the top left corner

			// Set up strata
			m_numberOfRandomSequences = 20;
			m_samplesPerStrata = 20;
			m_currentSample = 0;
			m_strataSequences = new int[m_model.m_pathDepth];
			generateRandomSequences();

			// Light sources
			generateLightSourcePoints();

			// Sphere points (for generating random rays and such)	
			generateDiffuseRayTable();
		}

		public bool isStillTracing() {
			return m_tracing;
		}

		public void traceLine() {
			// Trace a line
			m_tracing = true;
			if( m_currentLine >= m_model.m_height ) {
				return;
			}

			// Trace current line (actually, 2 lines at a time to increase coherence)
			for( int x = 0; x < m_model.m_width; x++ ) {
				double u = (double)x / (double)m_model.m_width;
				double v = (double)m_currentLine / (double)m_model.m_height;
				tracePixel( u, v, m_pixels[x + m_currentLine * m_model.m_width] );
				v = (double)( m_currentLine + 1 ) / (double)m_model.m_height;
				tracePixel( u, v, m_pixels[x + ( m_currentLine + 1 ) * m_model.m_width] );
			}

			// Finished image?
			m_currentLine += 2;
			if( m_currentLine >= m_model.m_height ) {
				m_tracing = false;
			}
		}

		// Update the bitmap with the data that's been computed so far.  The image is normalized and
		// this can be controlled via the 'saturation' value.
		public void updateBitmap() {
			double max = 0, invMax;
			for( int i = 0; i < m_currentLine * m_model.m_width; i++ ) {
				for( int j = 0; j < 3; j++ ) {
					if( max < m_pixels[i][j] ) {
						max = m_pixels[i][j];
					}
				}
			}
			max = max * m_model.m_saturation;
			invMax = 255.0d / max;
			for( int i = 0; i < m_currentLine * m_model.m_width; i++ ) {
				double pixelMax = Utils.maximum3( m_pixels[i][0], m_pixels[i][1], m_pixels[i][2] );
				double pixelMultiplier = invMax;
				if( pixelMax > max ) {
					pixelMultiplier *= max / pixelMax;
				}
				m_bitmap[i][0] = (byte)( m_pixels[i][2] * pixelMultiplier ); // B
				m_bitmap[i][1] = (byte)( m_pixels[i][1] * pixelMultiplier ); // G
				m_bitmap[i][2] = (byte)( m_pixels[i][0] * pixelMultiplier ); // R
			}
		}

		// Return the progress of the ray-tracer in the range [0,1]
		public double getProgress() {
			return (double)m_currentLine / (double)m_model.m_height;
		}

		void tracePixel( double u, double v, double[] pixel ) {
			double[] stratumSize = new double[2];
			stratumSize[0] = m_pixelSize[0] / m_model.m_raysPerPixel;
			stratumSize[1] = m_pixelSize[1] / m_model.m_raysPerPixel;
			for( int i = 0; i < m_model.m_pathDepth; i++ ) {
				m_strataSequences[i] = rand.Next( 0, m_numberOfRandomSequences );
			}
			m_currentSample = 0;
			for( int uStratum = 0; uStratum < m_model.m_raysPerPixel; uStratum++ )
				for( int vStratum = 0; vStratum < m_model.m_raysPerPixel; vStratum++ ) {
					double[] target = new double[3];
					double[] suVec = new double[3];
					double[] svVec = new double[3];
					double[] dir = new double[3];

					Utils.setf3( suVec, m_uVec );
					Utils.setf3( svVec, m_vVec );
					Utils.scale( suVec, u + ( (double)uStratum + rand.NextDouble() ) * stratumSize[0] );
					Utils.scale( svVec, v + ( (double)vStratum + rand.NextDouble() ) * stratumSize[1] );
					Utils.add3f( target, m_corner, suVec );
					Utils.add3f( target, svVec );
					Utils.sub3f( dir, target, m_model.m_eye );
					Utils.normalise( dir );
					traceRay( m_model.m_eye, dir, pixel );
					m_currentSample++;
				}
			Utils.scale( pixel, 1.0d / (double)m_model.m_raysPerPixel );
		}

		// Path tracing loop.
		// 
		// Consider a segment in the path, with a source and a target.
		// All that is known on entry is the source information.
		// The rayIntersect routine, brings out information about the
		// target.  A contribution of the target is then computed and
		// accumulated (into 'pixel').
		//
		// Input into loop:
		// - Source point : point at source
		// - Source direction : ray direction from which to gather intensity/radiance
		// - Source normal : normal at source  
		// - Source colour : how this gathered intensity/radiance will be transmitted
		// - Cosine Accumulation : dot product between normal and emitted ray.  This 
		//                         gives the Lambertian contribution. It is accumulated
		//                         for each bounce, since the light incident at an indirect
		//                         light source also follows the lambertian rule.
		//                         NOTE: This has been made redudant with the cosine-sampling
		//                               used.  It is now used to handle the 2x factor 
		//                               introduced by the importance sampling.
		// 
		// Special values are set on entry, since the initial source is not a surface point.
		// - Source point : eye point
		// - Source dir : ray direction associated with pixel (sub)sample
		// - Source normal : same as direction, so that dot product returns 1 (i.e., it's nullified)
		// - Source colour : { 1, 1, 1 } since we want the light emitted
		// - Cosine Accumulation : 1 since no bounces have ocurred yet
		void traceRay( double[] origin, double[] dir, double[] pixel ) {
			// Initial conditions
			double[] sourcePoint = new double[3];
			double[] sourceDir = new double[3];
			double[] sourceNormal = new double[3];

			Utils.setf3( sourcePoint, origin );
			Utils.setf3( sourceDir, dir );
			Utils.setf3( sourceNormal, dir );
			Utils.normalise( sourceNormal );
			double[] sourceColour = { 1.0d, 1.0d, 1.0d };
			double cosineAccumulation = 1.0d;

			// Trace path
			for( int depth = 0; depth < m_model.m_pathDepth; depth++ ) {
				int hitObjectIndex;
				double[] targetIntersectionPoint = new double[3];
				double[] targetNormal = new double[3];
				if( m_model.rayIntersect( sourcePoint, sourceDir, out hitObjectIndex, targetIntersectionPoint, targetNormal ) ) {
					double[] direct = new double[3]; // Direct contribution at target
					BaseObject hitObject = m_model.m_objects[hitObjectIndex];
					bool isLightSource = hitObject.m_lightSource;
					if( isLightSource ) {
						Utils.setf3( direct, hitObject.m_colour );
					} else {
						computeDirectContribution( targetIntersectionPoint, targetNormal, hitObjectIndex, direct );
					}
					// The indirect colour that is transmitted FROM the source point:
					double[] transmittedColour = new double[3];
					Utils.mul3f( transmittedColour, sourceColour, direct ); // Apply source reflectance/absorption

					// Finally, scale the transmitted colour by the accumulated cosine factor
					Utils.scale( transmittedColour, cosineAccumulation );

					// Add contribution
					Utils.add3f( pixel, transmittedColour );

					// Stop recursion, since we assume light sources don't "reflect" light
					if( isLightSource ) {
						break;
					}

					if( depth + 1 != m_model.m_pathDepth ) { // Generate another path segment?
						// Prepare next ray
						Utils.setf3( sourcePoint, targetIntersectionPoint );
						Utils.setf3( sourceNormal, targetNormal );
						// Generate random direction to sample in
						double e = rand.NextDouble();
						if( e < hitObject.m_diffuse ) {
							getDiffuseRay( targetNormal, depth, sourceDir );
						} else if( e < hitObject.m_diffuse + hitObject.m_specular ) {
							double[] targetReflection = new double[3];
							Utils.reflect( sourceDir, targetNormal, targetReflection );
							if( hitObject.m_perfectSpecular ) {
								Utils.setf3( sourceDir, targetReflection );
							} else {
								getSpecularRay( targetReflection, hitObject.m_specularIndex, sourceDir ); // TODO: (N + 2)/2PI scaling for reflection
							}
							if( Utils.dot( sourceDir, targetNormal ) <= 0 ) {
								break; // The reflected direction goes behind the object
							}
						} else if( e < hitObject.m_diffuse + hitObject.m_specular + hitObject.m_transmission ) {
							double[] targetRefraction = new double[3];
							double[] lv = new double[3];
							Utils.neg3f( lv, sourceDir );
							Utils.refract( lv, targetNormal, 1.0d, hitObject.m_indexOfRefraction, targetRefraction );
							if( hitObject.m_perfectSpecular )
								Utils.setf3( sourceDir, targetRefraction );
							if( Utils.dot( sourceDir, targetNormal ) >= 0 )
								break; // The refracted direction goes in front of the object
						} // Otherwise it is absorbed
						if( !( ( hitObject.m_specular > 0 || hitObject.m_transmission > 0 ) && hitObject.m_perfectSpecular ) ) {
							cosineAccumulation *= 0.5d; //Utils.dot( targetNormal, sourceDir ); // Negate this, since this is from the perspective of the point that is hit
						}
						// Colour transmitted by new sourcePoint
						Utils.mul3f( sourceColour, m_model.m_objects[hitObjectIndex].m_colour, sourceColour );
					}
				} else
					break;
			}
		}

		// Direct contribution:
		// 1. Find a point on the light source (the point sources built from area light sources)
		// 2. See if intersectionPoint can see it
		// 3. If it can, then add direct contribution
		// 4. Average several samples to get accurate direct contribution	
		void computeDirectContribution( double[] intersectionPoint, double[] normal, int hitObjectIndex, double[] direct ) {
			int numberOfSamplesToTest = (int)( (double)m_numLights * m_model.m_lightSampleRatio );
			for( int i = 0; i < numberOfSamplesToTest; i++ ) {
				// Compute light vector
				PointLight light;
				if( numberOfSamplesToTest == m_numLights )
					light = m_lights[i]; // Make sure that every light source is visited
				else
					light = m_lights[rand.Next( 0, m_numLights )]; // Choose a random light source
				double[] lightVector = new double[3];
				Utils.sub3f( lightVector, light.m_point, intersectionPoint );

				// Can point see this light sample?
				// 1. Is the point behind the area light source polygon?
				double planeDistance = Utils.dot( light.m_plane, intersectionPoint ) - light.m_plane[3];

				// 2. Is the light source blocked from view (or vice versa)
				if( planeDistance > 0 && m_model.testShadowRay( intersectionPoint, lightVector ) ) {
					Utils.normalise( lightVector );
					double diffuseContribution = Utils.dot( normal, lightVector );
					if( diffuseContribution > 0 ) {
						double[] localColour = new double[3];
						// Compute local diffuse lighting
						BaseObject obj = m_model.m_objects[hitObjectIndex];
						Utils.mul3f( localColour, m_model.m_objects[light.m_objectIndex].m_colour, obj.m_colour );
						double emissionDistribution = -Utils.dot( light.m_plane, lightVector );

						// Put it all together
						Utils.scale( localColour, emissionDistribution * diffuseContribution * obj.m_diffuse );
						Utils.add3f( direct, localColour );
					}
				}
			}
			Utils.scale( direct, 1.0d / (double)numberOfSamplesToTest );
		}

		Model m_model;
		bool m_tracing = true;
		byte[][] m_bitmap;
		int m_currentLine;
		double[][] m_pixels;
		double[] m_pixelSize = new double[2];

		// Camera
		double[] m_uVec = new double[3];
		double[] m_vVec = new double[3];
		double[] m_corner = new double[3];

		// Light sources

		// Generate random points on the light sources.  This creates points in
		// such a manner, that larger light sources get proportionally more
		// points.
		void generateLightSourcePoints() {
			// Find area of smallest lightsource triangle and total light source area
			double minArea = double.MaxValue, totalArea = 0;
			for( int i = 0; i < m_model.m_numObjects; i++ ) {
				if( m_model.m_objects[i].m_lightSource ) {
					double[][] tri = new double[3][] { new double[3], new double[3], new double[3] };
					for( int j = 0; j < m_model.m_objects[i].m_numFaces; j++ ) {
						m_model.getTriangle( i, j, tri );
						double triArea = Utils.triangleArea( tri );
						if( triArea < minArea )
							minArea = triArea;
						totalArea += triArea;
					}
				}
			}

			// Temporary structure for light sources and their object associations
			List<PointLight> lightList = new List<PointLight>();

			int lightSamples = m_model.m_lightSamples;
			// Generate points for light source
			for( int i = 0; i < m_model.m_numObjects; i++ ) {
				if( m_model.m_objects[i].m_lightSource ) {
					double[][] tri = new double[3][] { new double[3], new double[3], new double[3] };
					BaseObject obj = m_model.m_objects[i];
					for( int j = 0; j < obj.m_numFaces; j++ ) {
						m_model.getTriangle( i, j, tri );
						double triArea = Utils.triangleArea( tri );
						int numberOfSamples = (int)( ( triArea / minArea ) * (double)lightSamples + 0.01d ); // So the smallest triangle gets lightSamples samples, one that's n times bigger gets n*lightSamples, etc. (0.01f is just an epsilon)
						for( int k = 0; k < numberOfSamples; k++ ) {
							PointLight pl = new PointLight();
							Utils.generatePointOnTriangle( tri, pl.m_point );
							pl.m_objectIndex = i;
							pl.m_triangleIndex = j;
							Utils.setf3( pl.m_plane, obj.m_normals[j] );
							pl.m_plane[3] = obj.m_planeConstants[j];
							lightList.Add( pl );
						}
					}
				}
			}

			// Make an array copy of the point light souces
			m_numLights = lightList.Count;
			m_lights = lightList.ToArray();
		}

		PointLight[] m_lights; ///< Point light sources (note: these are ructed from emitter polygons to approximate area light sources)	
		int m_numLights; ///< Number of PointLights in m_lights

		// Sample tables

		// The diffuse ray table is used to store a precomputed cosine weighted sample
		// distribution (built once, for efficiency).
		void generateDiffuseRayTable() {
			m_diffuseTable = new double[m_samplesPerStrata * m_model.m_raysPerPixel * m_model.m_raysPerPixel][];
			int i = 0;
			for( int stratum = 0; stratum < m_samplesPerStrata; stratum++ )
				for( int uStrata = 0; uStrata < m_model.m_raysPerPixel; uStrata++ )
					for( int vStrata = 0; vStrata < m_model.m_raysPerPixel; vStrata++ ) {
						double u = ( (double)uStrata + rand.NextDouble() ) / (double)m_model.m_raysPerPixel;
						double v = ( (double)vStrata + rand.NextDouble() ) / (double)m_model.m_raysPerPixel;
						double theta = 2 * 3.1415d * u; // Azimuth
						double phi = Math.Acos( Math.Sqrt( v ) );
						m_diffuseTable[i] = new double[3];
						m_diffuseTable[i][0] = Math.Cos( theta ) * Math.Sin( phi );
						m_diffuseTable[i][1] = Math.Sin( theta ) * Math.Sin( phi );
						m_diffuseTable[i][2] = Math.Cos( phi );
						i++;
					}
		}

		// Generate random sequences of numbers.  This is used for the
		// diffuse sample stratification.
		void generateRandomSequences() {
			m_randomSequences = new int[m_numberOfRandomSequences][];
			int numRays = m_model.m_raysPerPixel * m_model.m_raysPerPixel;
			for( int i = 0; i < m_numberOfRandomSequences; i++ ) {
				m_randomSequences[i] = new int[numRays];
				for( int j = 0; j < numRays; j++ )
					m_randomSequences[i][j] = j;
				for( int j = 0; j < numRays; j++ ) { // Swap around sequence randomly
					int i1 = rand.Next( 0, numRays ), i2 = rand.Next( 0, numRays );
					int temp = m_randomSequences[i][i1];
					m_randomSequences[i][i1] = m_randomSequences[i][i2];
					m_randomSequences[i][i2] = temp;
				}
			}
		}

		// Get a diffuse ray (cosine weighted distribution).  The the ray is rotated so that
		// the distribution lookup will occur relative to the normal.
		void getDiffuseRay( double[] normal, int depth, double[] ray ) {
			int index = m_randomSequences[m_strataSequences[depth]][m_currentSample];//rand.Next(0, m_model.m_raysPerPixel) * m_model.m_raysPerPixel;
			int stratum = ( rand.Next( 0, m_samplesPerStrata ) ) * m_model.m_raysPerPixel * m_model.m_raysPerPixel;
			double[] ray1 = new double[3];
			double[] ray2 = new double[3];
			Utils.setf3( ray1, m_diffuseTable[( index + stratum )] );
			// Rotate ray1 to distribution of normal vector	(output: ray)
			double el = -Math.Acos( normal[2] );
			double az = -Math.Atan2( normal[1], normal[0] );
			// Y Rot
			ray2[0] = Math.Cos( el ) * ray1[0] - Math.Sin( el ) * ray1[2]; // TODO: Factor out Math.Sin and Math.Cos
			ray2[1] = ray1[1];
			ray2[2] = Math.Sin( el ) * ray1[0] + Math.Cos( el ) * ray1[2];
			// Z Rot	
			ray[0] = Math.Cos( az ) * ray2[0] + Math.Sin( az ) * ray2[1];
			ray[1] = -Math.Sin( az ) * ray2[0] + Math.Cos( az ) * ray2[1];
			ray[2] = ray2[2];
			//Utils.normalise( ray );	
		}

		// Generate a specular ray, around cosine lobe relative to the 'reflection' direction, based on
		// the specular index. This is not precomputed, since each imperfect specular surface would have
		// to have it's own samples due to the varying specular index.
		void getSpecularRay( double[] reflection, double specularIndex, double[] ray ) {
			double[] ray1 = new double[3];
			double[] ray2 = new double[3];
			// Set ray1 as the distributed ray wrt a (0, 0, 1) focus
			double theta = 2 * 3.1415d * rand.NextDouble(); // Azimuth
			double phi = Math.Acos( Math.Sqrt( Math.Pow( rand.NextDouble(), 1.0d / ( specularIndex + 1 ) ) ) ); // Elevation N = 0 is just a uniform sphere distribution
			ray1[0] = Math.Cos( theta ) * Math.Sin( phi );
			ray1[1] = Math.Sin( theta ) * Math.Sin( phi );
			ray1[2] = Math.Cos( phi );
			// Rotate ray1 to distribution of normal vector	(output: ray)
			double el = -Math.Acos( reflection[2] );
			double az = -Math.Atan2( reflection[1], reflection[0] );
			// Y Rot
			ray2[0] = Math.Cos( el ) * ray1[0] - Math.Sin( el ) * ray1[2]; // TODO: Factor out Math.Sin and Math.Cos
			ray2[1] = ray1[1];
			ray2[2] = Math.Sin( el ) * ray1[0] + Math.Cos( el ) * ray1[2];
			// Z Rot	
			ray[0] = Math.Cos( az ) * ray2[0] + Math.Sin( az ) * ray2[1];
			ray[1] = -Math.Sin( az ) * ray2[0] + Math.Cos( az ) * ray2[1];
			ray[2] = ray2[2];
			Utils.normalise( ray );
			double dp = Utils.dot( reflection, ray );
		}

		int m_samplesPerStrata;
		double[][] m_diffuseTable;
		int m_currentSample; ///< m_currentSample is the current sample position (i.e., which strata or ray)
		int m_numberOfRandomSequences;
		int[][] m_randomSequences;
		int[] m_strataSequences;
	};
};

