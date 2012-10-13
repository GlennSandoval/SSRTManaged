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
using System.Collections.Generic;
using System.IO;

namespace SSRTManaged {

	class BaseObject {
		// Special
		public string m_name;

		// Material data
		public double m_diffuse = 1;
		public double m_specular;
		public double m_transmission;
		public double m_specularIndex = 3;
		public double[] m_colour = { 1.0, 1.0, 1.0 };
		public bool m_smooth;
		public bool m_perfectSpecular = true;
		public double m_indexOfRefraction = 1.1; ///< If it's refractive, it's usually going to be more dense than a vacuum ;-)

		// Geometry data
		public double[] m_bounds = new double[6];
		public int m_numVertices;
		public double[][] m_vertices;
		public int m_numFaces; ///< Number of triangles
		public int[][] m_indices;
		public bool m_lightSource;
		public double[][] m_normals;
		public double[] m_planeConstants; ///< Such that normal i ( &m_normals[ 3 * i ] ) is the coefficents of the plane normal.X = D, where D is m_planeCosntants[ i ]
		public double[][] m_vertexNormals;
		public double[] m_translation = { 0.0, 0.0, 0.0 };

	};

	class Model {

		public int load( string fileName ) {
			string[] inFile;
			try {
				inFile = File.ReadAllLines( fileName );
			} catch {
				return 1;
			}

			// Check file format
			if( inFile[0].Trim().CompareTo( "SSRT Input File" ) != 0 ) {
				return 2;
			}

			List<string> source = new List<string>();

			for( int i = 1; i < inFile.Length; i++ ) {
				string s = inFile[i];
				if( !string.IsNullOrWhiteSpace( s ) && !s.StartsWith( "#" ) ) {
					source.Add( s );
				}
			}

			List<BaseObject> scene = new List<BaseObject>();
			BaseObject currentObject = null;
			m_numObjects = 0;
			int numDefObjects = 0;
			bool target = false;

			string str, att, val;
			string[] parts;

			for( int lineIndex = 0; lineIndex < source.Count; lineIndex++ ) {
				str = source[lineIndex].ToLower();
				if( str.IndexOf( "=" ) == -1 ) {
					return 2;
				}

				parts = str.Split( '=' );
				att = parts[0].Trim();
				val = parts[1].Trim();

				// Handle attribute values
				if( att == "version" )                                      // VERSION
					m_version = int.Parse( val );
				else if( att == "eye" ) {                                   // EYE
					parts = val.Split( ' ' );
					m_eye[0] = double.Parse( parts[0] );
					m_eye[1] = double.Parse( parts[1] );
					m_eye[2] = double.Parse( parts[2] );
				} else if( att == "direction" ) {                           // DIRECTION
					parts = val.Split( ' ' );
					m_direction[0] = double.Parse( parts[0] );
					m_direction[1] = double.Parse( parts[1] );
					m_direction[2] = double.Parse( parts[2] );
				} else if( att == "target" ) {                              // TARGET ( direction by target)
					parts = val.Split( ' ' );
					m_direction[0] = double.Parse( parts[0] );
					m_direction[1] = double.Parse( parts[1] );
					m_direction[2] = double.Parse( parts[2] );
					target = true;
				} else if( att == "up" ) {                                  // UP
					parts = val.Split( ' ' );
					m_up[0] = double.Parse( parts[0] );
					m_up[1] = double.Parse( parts[1] );
					m_up[2] = double.Parse( parts[2] );
				} else if( att == "width" ) {                               // WIDTH
					m_width = int.Parse( val );
				} else if( att == "height" ) {                              // HEIGHT
					m_height = int.Parse( val );
				} else if( att == "lightsamples" ) {                        // LIGHTSAMPLES
					m_lightSamples = int.Parse( val );
				} else if( att == "lightsampleratio" ) {                    // LIGHTSAMPLERATIO
					m_lightSampleRatio = double.Parse( val );
				} else if( att == "raysperpixel" ) {                        // RAYSPERPIXEL
					m_raysPerPixel = int.Parse( val );
				} else if( att == "pathdepth" ) {                           // RAYSPERPIXEL
					m_pathDepth = int.Parse( val );
				} else if( att == "saturation" ) {                          // SATURATION
					m_saturation = int.Parse( val );
				} else if( att == "object" ) {                              // OBJECT
					currentObject = new BaseObject();
					scene.Add( currentObject );
					currentObject.m_name = val;
					m_numObjects++;
				} else if( att == "diffuse" ) {                             // DIFFUSE
					if( currentObject == null ) {
						return 3;
					}
					currentObject.m_diffuse = double.Parse( val );
				} else if( att == "specular" ) {                            // SPECULAR
					if( currentObject == null ) {
						return 3;
					}
					currentObject.m_specular = double.Parse( val );
				} else if( att == "transmission" ) {                        // TRANSMISSION
					if( currentObject == null ) {
						return 3;
					}
					currentObject.m_transmission = double.Parse( val );
				} else if( att == "indexofrefraction" ) {                   // INDEXOFREFRACTION
					if( currentObject == null ) {
						return 3;
					}
					currentObject.m_indexOfRefraction = double.Parse( val );
				} else if( att == "specularindex" ) {                       // SPECULARINDEX
					if( currentObject == null )
						return 3;
					currentObject.m_specularIndex = double.Parse( val );
				} else if( att == "perfectspecular" ) {                     // PERFECTSPECULAR
					if( currentObject == null ) {
						return 3;
					}

					if( val == "false" ) {
						currentObject.m_perfectSpecular = false;
					} else {
						currentObject.m_lightSource = false;
					}
				} else if( att == "translation" ) {                         // TRANSLATION
					if( currentObject == null ) {
						return 3;
					}
					parts = val.Split( ' ' );
					currentObject.m_translation[0] = double.Parse( parts[0] );
					currentObject.m_translation[1] = double.Parse( parts[1] );
					currentObject.m_translation[2] = double.Parse( parts[2] );
				} else if( att == "colour" || att == "color" ) {              // COLOUR
					if( currentObject == null ) {
						return 3;
					}
					parts = val.Split( ' ' );
					currentObject.m_colour[0] = double.Parse( parts[0] );
					currentObject.m_colour[1] = double.Parse( parts[1] );
					currentObject.m_colour[2] = double.Parse( parts[2] );
				} else if( att == "lightsource" ) {                         // LIGHTSOURCE
					if( currentObject == null ) {
						return 3;
					}
					if( val == "true" )
						currentObject.m_lightSource = true;
					else
						currentObject.m_lightSource = false;
				} else if( att == "smooth" ) {                              // SMOOTH
					if( currentObject == null ) {
						return 3;
					}
					if( val == "true" )
						currentObject.m_smooth = true;
					else
						currentObject.m_smooth = false;
				} else if( att == "faces" ) {                               // FACES
					if( currentObject == null ) {
						return 3;
					}
					int faces = int.Parse( val );
					currentObject.m_numFaces = faces;
				} else if( att == "vertices" ) {                            // VERTICES
					if( currentObject == null )
						return 3;
					currentObject.m_numVertices = int.Parse( val );
				} else if( att == "objectdef" ) {                           // OBJECTDEF
					// Search for name
					BaseObject obj = null;
					// Find object
					foreach( BaseObject bo in scene ) {
						if( bo.m_name == val ) {
							obj = bo;
						}
					}
					if( obj == null ) {
						return 4;
					}

					lineIndex++;

					// Load vertices
					obj.m_vertices = new double[obj.m_numVertices][];
					for( int j = 0; j < obj.m_numVertices; lineIndex++, j++ ) {
						obj.m_vertices[j] = new double[3];
						str = source[lineIndex].ToLower();
						parts = str.Split( ' ' );
						for( int k = 0; k < 3; k++ ) {
							obj.m_vertices[j][k] = double.Parse( parts[k] );
							obj.m_vertices[j][k] += obj.m_translation[k % 3];
						}
					}

					// Load indices
					obj.m_indices = new int[obj.m_numFaces][];
					for( int j = 0; j < obj.m_numFaces; lineIndex++, j++ ) {
						obj.m_indices[j] = new int[3];
						str = source[lineIndex].ToLower();
						parts = str.Split( ' ' );
						for( int k = 0; k < 3; k++ ) {
							obj.m_indices[j][k] = int.Parse( parts[k] );
						}
					}
					lineIndex--;
					numDefObjects++;
				} else {
					return 5;
				}
			}

			if( numDefObjects != m_numObjects ) {
				return 6; // Number of object declarations don't match the number of objects with data
			}

			// Fix up direction to point to target
			if( target ) {
				m_direction[0] -= m_eye[0];
				m_direction[1] -= m_eye[1];
				m_direction[2] -= m_eye[2];
			}

			// Now convert list into an array
			m_objects = scene.ToArray();

			// Generate auxiliary information
			generateNormals();
			generateBounds();
			m_isLoaded = true;
			return 0;
		}
		public bool isLoaded() {
			return m_isLoaded;
		}

		#region Geometry


		// Intersect a ray with the model.  The index of the object that is hit is returned (since this is
		// required to index into the relevant material data).  The intersection point is returned (since
		// the reflected/refracted ray will require this as an origin).  The normal is also returned (for computing  
		// local illumination, and for generating the next ray in the path in accordance to the correct distribution).
		// The normal is interpolated using the barycentric coordinates of the intersection, if the hit object is smooth.
		// True is returned iff the ray does intersect an object.
		public bool rayIntersect( double[] origin, double[] dir, out int hitObjectIndex, double[] intersectionPoint, double[] normal ) {
			double tMin = double.MaxValue;
			double uMin = double.MaxValue;
			double vMin = double.MaxValue;
			int nearestObject = -1, nearestTriangle = -1;
			for( int objectIndex = 0; objectIndex < m_numObjects; objectIndex++ ) {
				BaseObject obj = m_objects[objectIndex];
				double[] coord = new double[3];
				if( !RayBox.RayAABB( obj.m_bounds, origin, dir, coord ) ) // Trivial rejection of whole object (Bounding Box check)
					continue;
				double[][] tri = new double[3][] { new double[3], new double[3], new double[3] };
				for( int i = 0; i < obj.m_numFaces; i++ ) {
					getTriangle( objectIndex, i, tri );
					double t, u, v;
					if( rayTriangleTest( tri, origin, dir, out t, out u, out v ) ) {
						if( tMin > t ) {
							tMin = t;
							nearestObject = objectIndex;
							nearestTriangle = i;
							uMin = u;
							vMin = v;
						}
					}
				}
			}
			if( nearestObject != -1 ) { // Is there a nearest object, or did the ray just shoot off into infinity?
				hitObjectIndex = nearestObject;
				intersectionPoint[0] = origin[0] + tMin * dir[0];
				intersectionPoint[1] = origin[1] + tMin * dir[1];
				intersectionPoint[2] = origin[2] + tMin * dir[2];
				if( m_objects[nearestObject].m_smooth == false )
					Utils.setf3( normal, m_objects[nearestObject].m_normals[nearestTriangle] ); // Triangle normal
				else {
					double[][] normals = new double[3][] { new double[3], new double[3], new double[3] };
					getNormals( nearestObject, nearestTriangle, normals );
					double a = 1 - uMin - vMin;
					double b = uMin;
					double c = vMin;
					normal[0] = a * normals[0][0] + b * normals[1][0] + c * normals[2][0];
					normal[1] = a * normals[0][1] + b * normals[1][1] + c * normals[2][1];
					normal[2] = a * normals[0][2] + b * normals[1][2] + c * normals[2][2];
					Utils.normalise( normal ); // Interpolated normal
				}
				return true;
			} else
				hitObjectIndex = -1;
			return false;
		}

		// Check a ray against a triangle.
		// Code by Thomas Akenine-Moller: http://www.realtimerendering.com
		public bool rayTriangleTest( double[][] tri, double[] s, double[] d, out double intersection, out  double barU, out double barV ) {
			// Thomas Moller's triangle intersection code
			double[] edge1 = new double[3];
			double[] edge2 = new double[3];
			double[] tVec = new double[3];
			double[] pVec = new double[3];
			double[] qVec = new double[3];
			double det, invDet, u, v, tVal;

			intersection = -1;
			barU = -1;
			barV = -1;

			Utils.sub3f( edge1, tri[1], tri[0] );
			Utils.sub3f( edge2, tri[2], tri[0] );
			Utils.cross( pVec, d, edge2 );
			det = Utils.dot( edge1, pVec );
			Utils.sub3f( tVec, s, tri[0] );
			if( det > 0.0f ) {
				u = Utils.dot( tVec, pVec );
				if( u < 0.0f || u > det ) {
					return false;
				}
				Utils.cross( qVec, tVec, edge1 );
				v = Utils.dot( d, qVec );
				if( v < 0.0f || u + v > det ) {
					return false;
				}
			} else {
				return false;
			}
			invDet = 1.0f / det;
			tVal = Utils.dot( edge2, qVec ) * invDet;
			if( tVal >= m_epsilon ) {
				intersection = tVal;
				barU = u * invDet;
				barV = v * invDet;
				return true;
			}
			return false;
		}

		// Get the triangle belonging to object objectIndex at index triangleIndex.
		public void getTriangle( int objectIndex, int triangleIndex, double[][] tri ) {
			BaseObject obj = m_objects[objectIndex];
			Utils.setf3( tri[0], obj.m_vertices[obj.m_indices[triangleIndex][0]] );
			Utils.setf3( tri[1], obj.m_vertices[obj.m_indices[triangleIndex][1]] );
			Utils.setf3( tri[2], obj.m_vertices[obj.m_indices[triangleIndex][2]] );
		}

		// Get the triangle normal belonging to the triangle in object objectIndex at index triangleIndex.
		public void getNormals( int objectIndex, int triangleIndex, double[][] normals ) {
			BaseObject obj = m_objects[objectIndex];
			Utils.setf3( normals[0], obj.m_vertexNormals[obj.m_indices[triangleIndex][0]] );
			Utils.setf3( normals[1], obj.m_vertexNormals[obj.m_indices[triangleIndex][1]] );
			Utils.setf3( normals[2], obj.m_vertexNormals[obj.m_indices[triangleIndex][2]] );
		}

		// Shadow ray test.  Returns true if shadowed, or false otherwise.
		// No other information is needed.
		public bool testShadowRay( double[] origin, double[] dir ) {
			for( int objectIndex = 0; objectIndex < m_numObjects; objectIndex++ ) {
				BaseObject obj = m_objects[objectIndex];
				double[] coord = new double[3];
				if( !RayBox.RayAABB( obj.m_bounds, origin, dir, coord ) ) // Trivial rejection of whole object
					continue;
				double[][] tri = new double[3][] { new double[3], new double[3], new double[3] };
				for( int i = 0; i < obj.m_numFaces; i++ ) {
					getTriangle( objectIndex, i, tri );
					double t, u, v;
					if( rayTriangleTest( tri, origin, dir, out t, out u, out v ) && t < 1 - m_epsilon ) // t < 1 will check the segment.
						return false;
				}
			}
			return true;
		}

		public double m_epsilon = 0.0001f;
		public int m_version;
		public int m_numObjects;
		public BaseObject[] m_objects;
		#endregion

		#region Camera
		public double[] m_eye = { 0.0f, 0.0f, 0.0f };
		public double[] m_direction = { 1.0f, 0.0f, 0.0f };
		public double[] m_up = { 0.0f, 1.0f, 0.0f };
		#endregion

		#region Image
		public int m_width, m_height;
		public double m_saturation;
		#endregion

		#region Light control
		public int m_lightSamples = 5;
		public double m_lightSampleRatio = 1.0f;
		#endregion

		#region Sample control
		public int m_raysPerPixel;
		public int m_pathDepth = 1;
		#endregion

		// Generate normals for each object.  Triangle normals are computed for ALL objects.
		// Vertex normals are computed for those objects that are "smooth".
		void generateNormals() {
			for( int objectIndex = 0; objectIndex < m_numObjects; objectIndex++ ) {
				BaseObject obj = m_objects[objectIndex];
				double[][] tri = new double[3][] { new double[3], new double[3], new double[3] };
				obj.m_normals = new double[obj.m_numFaces][]; // One normal per face 
				for( int i = 0; i < obj.m_numFaces; i++ ) {
					obj.m_normals[i] = new double[3];
				}
				obj.m_planeConstants = new double[obj.m_numFaces]; // One constant per face
				for( int i = 0; i < obj.m_numFaces; i++ ) {
					getTriangle( objectIndex, i, tri );
					Utils.triangleNormal( tri, obj.m_normals[i] );
					Utils.normalise( obj.m_normals[i] );
					obj.m_planeConstants[i] = Utils.dot( obj.m_normals[i], tri[0] ); // Compute plane constant of triangle
				}
				if( obj.m_smooth ) {
					obj.m_vertexNormals = new double[obj.m_numVertices][]; // One normal per vertex
					for( int i = 0; i < obj.m_numVertices; i++ ) {
						obj.m_vertexNormals[i] = new double[3];
					}
					for( int i = 0; i < obj.m_numFaces; i++ ) { // Add up normal
						Utils.add3f( obj.m_vertexNormals[obj.m_indices[i][0]], obj.m_normals[i] ); // Add face normals to vertices
						Utils.add3f( obj.m_vertexNormals[obj.m_indices[i][1]], obj.m_normals[i] );
						Utils.add3f( obj.m_vertexNormals[obj.m_indices[i][2]], obj.m_normals[i] );
					}
					for( int i = 0; i < obj.m_numVertices; i++ ) {
						Utils.normalise( obj.m_vertexNormals[i] );
					}
				}
			}
		}

		// Generate the bounding boxes for each object.
		void generateBounds() {
			for( int objectIndex = 0; objectIndex < m_numObjects; objectIndex++ ) {
				double[] bounds = m_objects[objectIndex].m_bounds;
				double[][] vertices = m_objects[objectIndex].m_vertices;

				// Set bounds to be exactly the first vertex
				bounds[0] = vertices[0][0];
				bounds[1] = vertices[0][1];
				bounds[2] = vertices[0][2];
				bounds[3] = vertices[0][0];
				bounds[4] = vertices[0][1];
				bounds[5] = vertices[0][2];

				for( int i = 1; i < m_objects[objectIndex].m_numVertices; i++ ) {
					if( bounds[0] > vertices[i][0] ) {
						bounds[0] = vertices[i][0];
					}
					if( bounds[1] > vertices[i][1] ) {
						bounds[1] = vertices[i][1];
					}
					if( bounds[2] > vertices[i][2] ) {
						bounds[2] = vertices[i][2];
					}
					if( bounds[3] < vertices[i][0] ) {
						bounds[3] = vertices[i][0];
					}
					if( bounds[4] < vertices[i][1] ) {
						bounds[4] = vertices[i][1];
					}
					if( bounds[5] < vertices[i][2] ) {
						bounds[5] = vertices[i][2];
					}
				}
			}
		}

		bool m_isLoaded;
	};
}

